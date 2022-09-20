// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Fluxzy.Misc;

namespace Fluxzy.Core
{
    public class DefaultTcpConnection : ITcpConnection
    {
        private readonly TcpClient _client;

        public DefaultTcpConnection()
        {
            _client = new TcpClient();
        }
        
        public async Task<IPEndPoint> ConnectAsync(IPAddress address, int port)
        {
            await _client.ConnectAsync(address, port);

            return (IPEndPoint) _client.Client.LocalEndPoint; 
        }

        public Stream GetStream()
        {
            var resultStream = 
                new  DisposeEventNotifierStream(_client.GetStream());

            resultStream.OnStreamDisposed += ResultStreamOnOnStreamDisposed;

            return resultStream;
        }

        private async Task ResultStreamOnOnStreamDisposed(object sender, StreamDisposeEventArgs args)
        {
            var stream = (DisposeEventNotifierStream) sender;
            stream.OnStreamDisposed -= ResultStreamOnOnStreamDisposed;

            await DisposeAsync(); 
        }

        public async ValueTask DisposeAsync()
        {
            _client?.Dispose();
        }
    }
}