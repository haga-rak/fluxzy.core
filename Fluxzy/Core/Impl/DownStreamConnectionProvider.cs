using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Fluxzy.Core
{
    internal class DownStreamConnectionProvider : IDownStreamConnectionProvider
    {
        private readonly List<TcpListener> _listeners;

        private readonly Channel<TcpClient> _pendingClientConnections =
            Channel.CreateBounded<TcpClient>(new BoundedChannelOptions(4));

        private CancellationToken _token;

        private readonly CancellationTokenSource _tokenSource = new();

        public DownStreamConnectionProvider(IEnumerable<ProxyBindPoint> boundPoints)
        {
            _listeners = boundPoints.Select(b => new TcpListener(b.EndPoint)).ToList();

            _token = _tokenSource.Token;
        }

        public async Task<TcpClient?> GetNextPendingConnection()
        {
            if (!_listeners.Any())
                return null;

            try
            {
                var nextConnection = await
                    _pendingClientConnections.Reader.ReadAsync(_token);

                return nextConnection;
            }
            catch (Exception)
            {
                // Listener Stop was probably called 
                return null;
            }
        }

        public IReadOnlyCollection<IPEndPoint>? ListenEndpoints { get; private set; } = Array.Empty<IPEndPoint>();

        public IReadOnlyCollection<IPEndPoint> Init(CancellationToken token)
        {
            _token = token;

            var boundEndPoints = new List<IPEndPoint>();

            foreach (var listener in _listeners)
            {
                try
                {
                    listener.Start(100);

                    boundEndPoints.Add((IPEndPoint)listener.LocalEndpoint);

                    var listenerCopy = listener;

                    HandleAcceptConnection(listenerCopy);

                 //   Task.Run(async () => await HandleAcceptConnection(listenerCopy));
                }
                catch (SocketException sex)
                {
                    throw new Exception("Impossible port : " +
                                        $"{((IPEndPoint)listener.LocalEndpoint).Address} - " +
                                        $"{((IPEndPoint)listener.LocalEndpoint).Port}  - \r\n"
                                        + sex, sex);
                }
            }

            if (!boundEndPoints.Any())
                throw new InvalidOperationException("No listen endpoints was provided");

            ListenEndpoints = boundEndPoints;
            return ListenEndpoints;
        }

        private async void HandleAcceptConnection(TcpListener listener)
        {
            try
            {
                while (true)
                {
                    var tcpClient = await listener.AcceptTcpClientAsync().ConfigureAwait(false);

                    tcpClient.NoDelay = true; // NO Delay for local connection
                    // tcpClient.ReceiveTimeout = 500; // We forgot connection after receiving.
                    tcpClient.ReceiveBufferSize = 1024 * 64;
                    tcpClient.SendBufferSize = 32 * 1024;
                    tcpClient.SendTimeout = 200;

                    await _pendingClientConnections.Writer.WriteAsync(tcpClient, _token);
                }
            }
            catch (Exception)
            {
                // Connection closed 
            }
        }

        public void Dispose()
        {
            foreach (var listener in _listeners)
                try
                {
                    listener.Stop();
                }
                catch (Exception)
                {
                    // Ignore errors
                }

            _tokenSource.Cancel();
        }

    }
}