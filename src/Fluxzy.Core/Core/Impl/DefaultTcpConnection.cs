// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Core
{
    internal class DefaultTcpConnection : ITcpConnection
    {
        private readonly TcpClient _client;

        public DefaultTcpConnection()
        {
            _client = new TcpClient();
            _client.NoDelay = true;
        }

        public async Task<IPEndPoint> ConnectAsync(IPAddress address, int port)
        {
            try {
                await _client.ConnectAsync(address, port).ConfigureAwait(false);

                return (IPEndPoint) _client.Client.LocalEndPoint!;
            }
            catch (Exception ex) {
                if (ex is AggregateException aggregateException && aggregateException.InnerExceptions.Any()) {
                    throw aggregateException.InnerExceptions.First();
                }

                throw;
            }
        }

        public Stream GetStream()
        {
            var resultStream =
                new DisposeEventNotifierStream(_client.GetStream());

            resultStream.OnStreamDisposed += ResultStreamOnOnStreamDisposed;

            return resultStream;
        }

        public void OnKeyReceived(string nssKey)
        {
            // Ignore
        }

        public ValueTask DisposeAsync()
        {
            _client?.Dispose();

            return default;
        }

        private ValueTask ResultStreamOnOnStreamDisposed(object sender, StreamDisposeEventArgs args)
        {
            var stream = (DisposeEventNotifierStream) sender;
            stream.OnStreamDisposed -= ResultStreamOnOnStreamDisposed;

            return DisposeAsync();
        }
    }
}
