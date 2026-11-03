// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Core
{
    public class UpstreamSocketReusabilityTests
    {
        [Fact]
        public async Task Idle_Connected_Socket_Is_Reusable()
        {
            await using var pair = await SocketPair.Create();

            await Task.Delay(50);

            Assert.False(pair.Stream.IsReadableWhileIdle());
        }

        [Fact]
        public async Task Socket_Is_Not_Reusable_After_Peer_Fin()
        {
            await using var pair = await SocketPair.Create();

            pair.Server.Client.Shutdown(SocketShutdown.Send);

            Assert.True(await Becomes(() => pair.Stream.IsReadableWhileIdle()));
        }

        [Fact]
        public async Task Socket_Is_Not_Reusable_After_Peer_Reset()
        {
            await using var pair = await SocketPair.Create();

            pair.Server.Client.LingerState = new LingerOption(true, 0);
            pair.Server.Close();

            Assert.True(await Becomes(() => pair.Stream.IsReadableWhileIdle()));
        }

        [Fact]
        public async Task Socket_Is_Not_Reusable_With_Unsolicited_Data_Pending()
        {
            await using var pair = await SocketPair.Create();

            await pair.Server.GetStream().WriteAsync(new byte[] { 0x15 });

            Assert.True(await Becomes(() => pair.Stream.IsReadableWhileIdle()));
        }

        [Fact]
        public async Task Disposed_Stream_Is_Not_Reusable_And_Does_Not_Throw()
        {
            await using var pair = await SocketPair.Create();

            await pair.Stream.DisposeAsync();

            Assert.True(pair.Stream.IsReadableWhileIdle());
        }

        private static async Task<bool> Becomes(Func<bool> condition)
        {
            var watch = Stopwatch.StartNew();

            while (watch.ElapsedMilliseconds < 2000) {
                if (condition())
                    return true;

                await Task.Delay(10);
            }

            return false;
        }

        private sealed class SocketPair : IAsyncDisposable
        {
            private readonly TcpListener _listener;
            private readonly TcpClient _client;

            private SocketPair(TcpListener listener, TcpClient client, TcpClient server, DisposeEventNotifierStream stream)
            {
                _listener = listener;
                _client = client;
                Server = server;
                Stream = stream;
            }

            public TcpClient Server { get; }

            public DisposeEventNotifierStream Stream { get; }

            public static async Task<SocketPair> Create()
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();

                var client = new TcpClient();
                var connect = client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint) listener.LocalEndpoint).Port);
                var server = await listener.AcceptTcpClientAsync();
                await connect;

                return new SocketPair(listener, client, server, new DisposeEventNotifierStream(client, null));
            }

            public async ValueTask DisposeAsync()
            {
                try {
                    await Stream.DisposeAsync();
                }
                catch {
                }

                Server.Dispose();
                _client.Dispose();
                _listener.Stop();
            }
        }
    }
}
