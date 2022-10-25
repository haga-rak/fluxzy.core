using Fluxzy.Misc;
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
            Channel.CreateUnbounded<TcpClient>(new UnboundedChannelOptions()
            {
                SingleWriter = true,
                SingleReader = true
            }); 

        private CancellationToken _token;

        private readonly CancellationTokenSource _tokenSource = new();

        public DownStreamConnectionProvider(IEnumerable<ProxyBindPoint> boundPoints)
        {
            _listeners = boundPoints.Select(b => new TcpListener(b.EndPoint)).ToList();

            _token = _tokenSource.Token;
        }

        public async ValueTask<TcpClient?> GetNextPendingConnection()
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
                    listener.Start(200);

                    boundEndPoints.Add((IPEndPoint) listener.LocalEndpoint);

                    var listenerCopy = listener;

                    new Thread((a) => HandleAcceptConnection((TcpListener)a))
                    {
                        IsBackground = true,
                        Priority = ThreadPriority.Normal
                    }.Start(listenerCopy);
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

        private void HandleAcceptConnection(TcpListener listener)
        {
            try
            {
                listener.BeginAcceptTcpClient(Callback, listener);
            }
            catch (Exception)
            {
                // Connection closed 
            }
        }

        private void Callback(IAsyncResult ar)
        {
            try
            {
                var listener = (TcpListener) ar.AsyncState;
                var tcpClient = listener.EndAcceptTcpClient(ar);
                listener.BeginAcceptTcpClient(Callback, listener);
                tcpClient.NoDelay = true;
                _pendingClientConnections.Writer.TryWrite(tcpClient);
            }
            catch (Exception)
            {
                // ignored
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