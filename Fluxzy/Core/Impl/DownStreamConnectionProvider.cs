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

        private CancellationTokenSource _tokenSource = new();

        private CancellationToken _token;

        public DownStreamConnectionProvider(IEnumerable<ProxyBindPoint> boundPoints)
        {
            _listeners = boundPoints.Select(b => 
                new TcpListener(string.IsNullOrWhiteSpace(b.Address)
                    ? IPAddress.Any : IPAddress.Parse(b.Address) , b.Port)).ToList();

            _token = _tokenSource.Token; 
        }

        public void Dispose()
        {
            foreach (var listener in _listeners)
            {
                try
                {
                    listener.Stop();
                }
                catch (Exception)
                {
                    // Ignore errors
                }
            }

            _tokenSource.Cancel();
        }

        public void Init(CancellationToken token)
        {
            _token = token;
            foreach (var listener in _listeners)
            {

                try
                {
                    listener.Start(Int32.MaxValue);

                    var listenerCopy = listener;

                    Task.Run(async () => await HandleAcceptConnection(listenerCopy));

                }
                catch (SocketException sex)
                {
                    throw new Exception($"Impossible port : " +
                                        $"{((IPEndPoint)listener.LocalEndpoint).Address} - " +
                                        $"{((IPEndPoint) listener.LocalEndpoint).Port}  - \r\n" 
                        + sex.ToString(), sex) ;
                }
            }
        }

        private async Task HandleAcceptConnection(TcpListener listener)
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

            }
        }



        public async Task<TcpClient> GetNextPendingConnection()
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
    }
}