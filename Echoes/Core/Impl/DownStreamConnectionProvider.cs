﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Clients;

namespace Echoes.Core
{
    internal class DownStreamConnectionProvider : IDownStreamConnectionProvider
    {
        private TcpListener _listener;
        private CancellationToken _token;

        public DownStreamConnectionProvider(IPAddress bindingAddress, int port)
        {
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
                // Ignore errors
            }

            _listener = null;
        }

        public void Init(CancellationToken token)
        {
            _token = token;
            _listener.Start(Int32.MaxValue);
        }

        public async Task<TcpClient> GetNextPendingConnection()
        {
            if (_listener == null)
                return null;

            try
            { 
                TcpClient tcpClient = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);

                tcpClient.NoDelay = true;  // NO Delay for local connection
                // tcpClient.ReceiveTimeout = 500; // We forgot connection after receiving.
                tcpClient.ReceiveBufferSize = 1024 * 64;
                tcpClient.SendBufferSize = 32 * 1024;
                tcpClient.SendTimeout = 200;

                // var result = new TcpDownStreamConnection(tcpClient, utcNow, utcNow, _referenceClock);
                

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