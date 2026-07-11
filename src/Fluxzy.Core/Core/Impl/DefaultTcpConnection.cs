// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Core
{
    internal class DefaultTcpConnectionConnectResult : ITcpConnectionConnectResult
    {
        public DefaultTcpConnectionConnectResult(DisposeEventNotifierStream stream)
        {
            Stream = stream;
        }

        public DisposeEventNotifierStream Stream { get; }

        public void ProcessNssKey(string nssKey)
        {
        }
    }

    internal class DefaultTcpConnection : ITcpConnection
    {
        public Task<ITcpConnectionConnectResult> ConnectAsync(IPAddress address, int port)
            => ConnectAsync(address, port, UpstreamConnectOptions.None);

        public Task<ITcpConnectionConnectResult> ConnectAsync(
            IPAddress address, int port, UpstreamConnectOptions options)
            => ConnectAsync(address, port, options, CancellationToken.None);

        public async Task<ITcpConnectionConnectResult> ConnectAsync(
            IPAddress address, int port, UpstreamConnectOptions options, CancellationToken token)
        {
            TcpClient? client = null;

            try {
                client = new TcpClient(address.AddressFamily);
                client.NoDelay = true;
                options.Apply(client.Client, new IPEndPoint(address, port));
                await client.ConnectAsync(address, port, token).ConfigureAwait(false);
                var stream = new DisposeEventNotifierStream(client, null);
                return new DefaultTcpConnectionConnectResult(stream);
            }
            catch (Exception ex) {
                client?.Dispose();

                if (ex is ClientErrorException)
                    throw;

                if (ex is AggregateException aggregateException && aggregateException.InnerExceptions.Any()) {
                    throw aggregateException.InnerExceptions.First();
                }

                throw;
            }
        }
        
        public void OnKeyReceived(string nssKey)
        {
            // Ignore
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }
    }
}
