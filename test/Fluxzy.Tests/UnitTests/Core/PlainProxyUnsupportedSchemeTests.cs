// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Utils;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Core
{
    /// <summary>
    ///     A client whose system proxy covers every protocol sends ftp:// targets to
    ///     Fluxzy as FTP-over-HTTP gateway requests. Fluxzy used to dial port 80 of the
    ///     FTP host and report a connection failure (#821).
    /// </summary>
    public class PlainProxyUnsupportedSchemeTests
    {
        [Theory]
        [InlineData("ftp://ftp.example.com/pub/", "ftp")]
        [InlineData("gopher://example.com/", "gopher")]
        public async Task Absolute_Form_Non_Http_Scheme_Gets_Explicit_528(string target, string scheme)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();
            await using var proxy = new Proxy(setting);
            var proxyPort = proxy.Run().First().Port;

            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, proxyPort);
            var stream = client.GetStream();

            var request = $"GET {target} HTTP/1.1\r\nHost: ftp.example.com\r\n\r\n";
            await stream.WriteAsync(Encoding.ASCII.GetBytes(request));

            var response = await ReadToEnd(stream);

            Assert.StartsWith("HTTP/1.1 528", response);
            Assert.Contains($"x-fluxzy-network-error: {NetworkErrorCodes.UnsupportedScheme}", response);
            Assert.Contains($"cannot forward {scheme}://", response);
        }

        [Fact]
        public async Task Unsupported_Scheme_After_A_Valid_Request_On_Same_Connection()
        {
            await using var origin = new EchoOrigin();

            var setting = FluxzySetting.CreateLocalRandomPort();
            await using var proxy = new Proxy(setting);
            var proxyPort = proxy.Run().First().Port;

            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, proxyPort);
            var stream = client.GetStream();

            var first = $"GET http://127.0.0.1:{origin.Port}/ HTTP/1.1\r\nHost: 127.0.0.1:{origin.Port}\r\n\r\n";
            await stream.WriteAsync(Encoding.ASCII.GetBytes(first));

            var firstResponse = await ReadOne(stream);
            Assert.StartsWith("HTTP/1.1 200", firstResponse);

            var second = "GET ftp://ftp.example.com/ HTTP/1.1\r\nHost: ftp.example.com\r\n\r\n";
            await stream.WriteAsync(Encoding.ASCII.GetBytes(second));

            var secondResponse = await ReadToEnd(stream);

            Assert.StartsWith("HTTP/1.1 528", secondResponse);
            Assert.Contains($"x-fluxzy-network-error: {NetworkErrorCodes.UnsupportedScheme}", secondResponse);
        }

        [Theory]
        [InlineData("ftp://host/", true)]
        [InlineData("FTP://host/", true)]
        [InlineData("ws://host/", true)]
        [InlineData("http://host/", false)]
        [InlineData("https://host/", false)]
        [InlineData("/relative/path", false)]
        [InlineData("host:443", false)]
        [InlineData("*", false)]
        public void TryGetUnsupportedScheme_Detects_Only_Absolute_Non_Http(string path, bool expected)
        {
            var header = new RequestHeader(
                $"GET {path} HTTP/1.1\r\nHost: host\r\n\r\n".AsMemory(), false);

            Assert.Equal(expected, AuthorityUtility.TryGetUnsupportedScheme(header, out _));
        }

        private static async Task<string> ReadToEnd(NetworkStream stream)
        {
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);

            return Encoding.ASCII.GetString(memory.ToArray());
        }

        private static async Task<string> ReadOne(NetworkStream stream)
        {
            var buffer = new byte[65536];
            var total = 0;

            while (true) {
                var read = await stream.ReadAsync(buffer.AsMemory(total));

                if (read == 0)
                    throw new IOException("Proxy closed the connection");

                total += read;

                var text = Encoding.ASCII.GetString(buffer, 0, total);
                var headerEnd = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);

                if (headerEnd < 0)
                    continue;

                var contentLength = int.Parse(text.Substring(0, headerEnd).Split("\r\n")
                    .First(l => l.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                    .Split(':')[1].Trim());

                if (total >= headerEnd + 4 + contentLength)
                    return text;
            }
        }

        private sealed class EchoOrigin : IAsyncDisposable
        {
            private readonly TcpListener _listener;

            public EchoOrigin()
            {
                _listener = new TcpListener(IPAddress.Loopback, 0);
                _listener.Start();
                Port = ((IPEndPoint) _listener.LocalEndpoint).Port;
                _ = AcceptLoop();
            }

            public int Port { get; }

            private async Task AcceptLoop()
            {
                try {
                    while (true) {
                        var client = await _listener.AcceptTcpClientAsync();
                        _ = Serve(client);
                    }
                }
                catch (ObjectDisposedException) {
                }
                catch (SocketException) {
                }
            }

            private static async Task Serve(TcpClient client)
            {
                using (client) {
                    var stream = client.GetStream();
                    var buffer = new byte[8192];

                    while (true) {
                        var total = 0;

                        while (true) {
                            var read = await stream.ReadAsync(buffer.AsMemory(total));

                            if (read == 0)
                                return;

                            total += read;

                            if (Encoding.ASCII.GetString(buffer, 0, total).Contains("\r\n\r\n"))
                                break;
                        }

                        var response = "HTTP/1.1 200 OK\r\nContent-Length: 2\r\n\r\nok";
                        await stream.WriteAsync(Encoding.ASCII.GetBytes(response));
                    }
                }
            }

            public ValueTask DisposeAsync()
            {
                _listener.Stop();
                return default;
            }
        }
    }
}
