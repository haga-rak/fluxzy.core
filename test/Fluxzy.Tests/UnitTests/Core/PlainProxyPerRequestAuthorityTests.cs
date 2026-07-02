// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Core
{
    /// <summary>
    ///     Regression test for cross-authority routing on a keep-alive plain HTTP proxy
    ///     connection: Http11DownStreamPipe used to stamp every request after the first
    ///     with the connection-level authority, sending it to whichever host came first
    ///     (wrong body from permissive origins, 421 from strict ones).
    /// </summary>
    public class PlainProxyPerRequestAuthorityTests
    {
        [Fact]
        public async Task KeepAlive_Plain_Proxy_Connection_Routes_Each_Request_By_Its_Own_Authority()
        {
            await using var originA = new TaggedOrigin("server-a");
            await using var originB = new TaggedOrigin("server-b");

            var setting = FluxzySetting.CreateLocalRandomPort();
            await using var proxy = new Proxy(setting);
            var proxyPort = proxy.Run().First().Port;

            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, proxyPort);
            var stream = client.GetStream();

            // Three absolute-form requests on the same downstream connection,
            // alternating authorities
            var first = await SendAbsoluteFormRequest(stream, originA.Port);
            var second = await SendAbsoluteFormRequest(stream, originB.Port);
            var third = await SendAbsoluteFormRequest(stream, originA.Port);

            Assert.Equal($"server-a host=127.0.0.1:{originA.Port}", first);
            Assert.Equal($"server-b host=127.0.0.1:{originB.Port}", second);
            Assert.Equal($"server-a host=127.0.0.1:{originA.Port}", third);
        }

        private static async Task<string> SendAbsoluteFormRequest(NetworkStream stream, int targetPort)
        {
            var request =
                $"GET http://127.0.0.1:{targetPort}/resource HTTP/1.1\r\n" +
                $"Host: 127.0.0.1:{targetPort}\r\n\r\n";

            await stream.WriteAsync(Encoding.ASCII.GetBytes(request));

            var buffer = new byte[65536];
            var total = 0;
            int headerEnd;

            while (true) {
                var read = await stream.ReadAsync(buffer.AsMemory(total));

                if (read == 0)
                    throw new IOException("Proxy closed the connection");

                total += read;

                headerEnd = Encoding.ASCII.GetString(buffer, 0, total)
                                    .IndexOf("\r\n\r\n", StringComparison.Ordinal);

                if (headerEnd >= 0)
                    break;
            }

            var contentLength = int.Parse(Encoding.ASCII.GetString(buffer, 0, headerEnd)
                                                  .Split("\r\n")
                                                  .First(l => l.StartsWith("Content-Length:",
                                                      StringComparison.OrdinalIgnoreCase))
                                                  .Split(':')[1].Trim());

            var bodyStart = headerEnd + 4;

            while (total - bodyStart < contentLength) {
                var read = await stream.ReadAsync(buffer.AsMemory(total));

                if (read == 0)
                    throw new IOException("Proxy closed the connection mid-body");

                total += read;
            }

            return Encoding.ASCII.GetString(buffer, bodyStart, contentLength);
        }

        /// <summary>
        ///     Keep-alive origin answering every request with its tag and the received Host.
        /// </summary>
        private sealed class TaggedOrigin : IAsyncDisposable
        {
            private readonly TcpListener _listener;
            private readonly CancellationTokenSource _cts = new();
            private readonly string _tag;

            public TaggedOrigin(string tag)
            {
                _tag = tag;
                _listener = new TcpListener(IPAddress.Loopback, 0);
                _listener.Start();
                Port = ((IPEndPoint) _listener.LocalEndpoint).Port;
                _ = AcceptLoop();
            }

            public int Port { get; }

            private async Task AcceptLoop()
            {
                try {
                    while (!_cts.IsCancellationRequested) {
                        var client = await _listener.AcceptTcpClientAsync(_cts.Token).ConfigureAwait(false);
                        _ = Serve(client);
                    }
                }
                catch {
                    // shutting down
                }
            }

            private async Task Serve(TcpClient client)
            {
                try {
                    using var _ = client;
                    var stream = client.GetStream();
                    var buffer = new byte[16 * 1024];

                    while (!_cts.IsCancellationRequested) {
                        var total = 0;

                        while (true) {
                            var read = await stream.ReadAsync(buffer.AsMemory(total), _cts.Token)
                                                   .ConfigureAwait(false);

                            if (read == 0)
                                return;

                            total += read;

                            if (Encoding.ASCII.GetString(buffer, 0, total).Contains("\r\n\r\n"))
                                break;
                        }

                        var hostValue = Encoding.ASCII.GetString(buffer, 0, total)
                                                .Split("\r\n")
                                                .First(l => l.StartsWith("Host:", StringComparison.OrdinalIgnoreCase))
                                                .Split(':', 2)[1].Trim();

                        var body = $"{_tag} host={hostValue}";

                        var response =
                            $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n" +
                            $"Content-Length: {body.Length}\r\nConnection: keep-alive\r\n\r\n{body}";

                        await stream.WriteAsync(Encoding.ASCII.GetBytes(response), _cts.Token)
                                    .ConfigureAwait(false);
                    }
                }
                catch {
                    // client or shutdown
                }
            }

            public ValueTask DisposeAsync()
            {
                _cts.Cancel();
                _listener.Stop();
                _cts.Dispose();

                return default;
            }
        }
    }
}
