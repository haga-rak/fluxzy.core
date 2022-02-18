using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Echoes.Core
{
    internal class DownStreamConnectionProvider : IDownStreamConnectionProvider
    {
        private readonly IReferenceClock _referenceClock;
        private TcpListener _listener;
        private CancellationToken _token;

        private BufferBlock<TcpClient> _clients = new BufferBlock<TcpClient>();

        public DownStreamConnectionProvider(IPAddress bindingAddress, int port, IReferenceClock referenceClock)
        {
            _referenceClock = referenceClock;
            _listener = new TcpListener(bindingAddress, port);
        }

        public void Dispose()
        {
            try
            {
                _listener.Stop();
            }
            catch (Exception)
            {
                // 
            }

            _listener = null;
        }

        public void Init(CancellationToken token)
        {
            _token = token;
            _listener.Start();

            //Task.Run(() => InternalAcceptLoop(), token);
            //Task.Run(() => InternalAcceptLoop(), token);
            //Task.Run(() => InternalAcceptLoop(), token);
            //Task.Run(() => InternalAcceptLoop(), token);
        }

        private void InternalAcceptLoop()
        {
            try
            {
                while (true)
                {
                    var client = _listener.AcceptTcpClient();
                    _clients.Post(client);
                }
            }
            catch (Exception)
            {

            }
        }


        public async Task<TcpClient> GetNextPendingConnection()
        {
            if (_listener == null)
                return null;

            try
            {
                TcpClient tcpClient = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);

                tcpClient.NoDelay = true;  // NO Delay for local connection
                //tcpClient.ReceiveTimeout = 500; // We forgot connection after receiving.
                tcpClient.ReceiveBufferSize = 1024 * 64;
                tcpClient.SendBufferSize = 32 * 1024;

                var utcNow = _referenceClock.Instant();

                // var result = new TcpDownStreamConnection(tcpClient, utcNow, utcNow, _referenceClock);

                _token.Register(() => tcpClient.Dispose());

                return tcpClient; 
            }
            catch (Exception)
            {
                // Listener Stop was probably called 
                return null;
            }
        }
    }
}