// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Core;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Core
{
    public class UpgradeAdvertisementConnectionReuseTests
    {
        [Fact]
        public void Upgrade_Offer_On_200_Is_Not_A_Close_Request()
        {
            var flatHeader =
                "HTTP/1.1 200 OK\r\n" +
                "upgrade: h2,h2c\r\n" +
                "connection: Upgrade\r\n" +
                "content-length: 2\r\n\r\n";

            var header = new ResponseHeader(flatHeader.AsMemory(), true, true);

            Assert.False(header.ConnectionCloseRequest);
        }

        [Fact]
        public void Upgrade_On_101_Is_A_Close_Request()
        {
            var flatHeader =
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "upgrade: websocket\r\n" +
                "connection: Upgrade\r\n\r\n";

            var header = new ResponseHeader(flatHeader.AsMemory(), true, true);

            Assert.True(header.ConnectionCloseRequest);
        }

        [Fact]
        public async Task Connection_Is_Reused_When_Server_Offers_Upgrade()
        {
            await using var origin = new UpgradeAdvertisingOrigin();

            var setting = FluxzySetting.CreateLocalRandomPort();
            await using var proxy = new Proxy(setting);
            var endpoints = proxy.Run();

            using var httpClient = new HttpClient(new HttpClientHandler {
                Proxy = new WebProxy($"http://127.0.0.1:{endpoints.First().Port}"),
                UseProxy = true
            });

            for (var i = 0; i < 2; i++) {
                using var response = await httpClient.GetAsync($"http://127.0.0.1:{origin.Port}/");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("OK", await response.Content.ReadAsStringAsync());

                // recycling happens on a completion continuation, leave it time to land
                await Task.Delay(250);
            }

            Assert.Equal(1, origin.ConnectionCount);
        }

        private sealed class UpgradeAdvertisingOrigin : IAsyncDisposable
        {
            private readonly TcpListener _listener;
            private readonly CancellationTokenSource _cts = new();
            private readonly Task _loop;
            private int _connectionCount;

            public UpgradeAdvertisingOrigin()
            {
                _listener = new TcpListener(IPAddress.Loopback, 0);
                _listener.Start();
                Port = ((IPEndPoint) _listener.LocalEndpoint).Port;
                _loop = AcceptLoop();
            }

            public int Port { get; }

            public int ConnectionCount => Volatile.Read(ref _connectionCount);

            private async Task AcceptLoop()
            {
                try {
                    while (!_cts.IsCancellationRequested) {
                        var client = await _listener.AcceptTcpClientAsync(_cts.Token).ConfigureAwait(false);
                        Interlocked.Increment(ref _connectionCount);
                        _ = Serve(client);
                    }
                }
                catch {
                    // shutting down
                }
            }

            private static async Task Serve(TcpClient client)
            {
                try {
                    using (client) {
                        var stream = client.GetStream();
                        var buffer = new byte[4096];
                        var received = new StringBuilder();

                        while (true) {
                            var read = await stream.ReadAsync(buffer).ConfigureAwait(false);

                            if (read == 0)
                                return;

                            received.Append(Encoding.ASCII.GetString(buffer, 0, read));

                            while (received.ToString().Contains("\r\n\r\n")) {
                                var payload = "OK"u8.ToArray();

                                var header = Encoding.ASCII.GetBytes(
                                    "HTTP/1.1 200 OK\r\n" +
                                    "upgrade: h2,h2c\r\n" +
                                    "connection: Upgrade\r\n" +
                                    $"content-length: {payload.Length}\r\n" +
                                    "content-type: text/javascript\r\n\r\n");

                                await stream.WriteAsync(header).ConfigureAwait(false);
                                await stream.WriteAsync(payload).ConfigureAwait(false);
                                await stream.FlushAsync().ConfigureAwait(false);

                                var text = received.ToString();
                                var idx = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                                received.Clear();
                                received.Append(text.Substring(idx + 4));
                            }
                        }
                    }
                }
                catch {
                    // client gone
                }
            }

            public async ValueTask DisposeAsync()
            {
                _cts.Cancel();
                _listener.Stop();

                try {
                    await _loop.ConfigureAwait(false);
                }
                catch {
                    // ignore
                }

                _cts.Dispose();
            }
        }
    }
}
