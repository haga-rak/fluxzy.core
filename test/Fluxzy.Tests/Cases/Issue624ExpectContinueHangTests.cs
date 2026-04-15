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

namespace Fluxzy.Tests.Cases
{
    /// <summary>
    ///     Regression coverage for https://github.com/haga-rak/fluxzy.core/issues/624:
    ///     AWS CLI S3 uploads hang because the request uses
    ///     <c>Expect: 100-continue</c> with <c>Transfer-Encoding: chunked</c>.
    ///
    ///     Before the fix, Fluxzy (a) stripped <c>Expect</c> on the upstream leg
    ///     and (b) pumped the client body sequentially before reading any
    ///     response header — so the proxy ended up blocked in CopyDetailed()
    ///     on a client that was itself waiting for the interim response.
    ///
    ///     After the fix, the HTTP/1.1 pool:
    ///       - forwards <c>Expect: 100-continue</c> to HTTP/1.1 upstreams;
    ///       - waits up to <c>ExpectContinueTimeout</c> for a 1xx/final
    ///         response before pumping the body;
    ///       - forwards any interim the origin sends back to the client;
    ///       - synthesises a 100 Continue if the origin stays silent so
    ///         naive clients don't stall indefinitely (nginx/Apache parity).
    ///
    ///     Tests stay on plain HTTP (no ReverseSecure/TLS) and drive the proxy
    ///     with a raw TCP socket so the wire behaviour is observable.
    ///     HttpClient would mask the bug because .NET silently sends the body
    ///     after <c>Expect100ContinueTimeout</c> (default 1 s) expires.
    /// </summary>
    public class Issue624ExpectContinueHangTests
    {
        [Fact]
        public async Task Expect100Continue_WithSilentOrigin_ProxySynthesisesInterimResponse()
        {
            using var upstreamCts = new CancellationTokenSource();

            // Silent upstream — accepts the TCP connection and drains bytes
            // but never answers. Reproduces an HTTP/1.0 or Expect-ignorant
            // origin: the proxy must synthesise its own 100 Continue,
            // otherwise the client deadlocks.
            var upstream = new TcpListener(IPAddress.Loopback, 0);
            upstream.Start();
            var upstreamPort = ((IPEndPoint) upstream.LocalEndpoint).Port;

            var upstreamTask = Task.Run(async () => {
                try {
                    using var upstreamClient =
                        await upstream.AcceptTcpClientAsync(upstreamCts.Token);
                    using var upstreamStream = upstreamClient.GetStream();

                    var buffer = new byte[4096];
                    while (!upstreamCts.IsCancellationRequested) {
                        var read = await upstreamStream.ReadAsync(buffer, upstreamCts.Token);

                        if (read == 0)
                            break;
                    }
                }
                catch {
                    // Cancellation or socket abort is expected on teardown.
                }
            });

            var setting = FluxzySetting.CreateLocalRandomPort()
                                       .SetExpectContinueTimeout(TimeSpan.FromMilliseconds(300));

            await using var proxy = new Proxy(setting);

            var (clientStream, _) = await ConnectRawClient(proxy);

            await SendExpectContinueRequest(clientStream, upstreamPort);

            var received = await ReadInterimResponse(clientStream, TimeSpan.FromSeconds(8));

            upstreamCts.Cancel();
            upstream.Stop();
            try { await upstreamTask; } catch { /* teardown */ }

            Assert.Contains("HTTP/1.1 100", received);
            Assert.Contains("Continue", received, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Expect100Continue_OriginSends100_ProxyForwardsItThenFinalResponse()
        {
            using var upstreamCts = new CancellationTokenSource();

            // Cooperative upstream: on seeing `Expect: 100-continue`, it
            // replies `HTTP/1.1 100 Continue`, consumes the chunked body and
            // answers `200 OK`. This exercises the RFC-correct path where
            // the proxy forwards the origin's interim to the client.
            var upstream = new TcpListener(IPAddress.Loopback, 0);
            upstream.Start();
            var upstreamPort = ((IPEndPoint) upstream.LocalEndpoint).Port;

            var upstreamTask = Task.Run(async () => {
                try {
                    using var upstreamClient =
                        await upstream.AcceptTcpClientAsync(upstreamCts.Token);
                    using var upstreamStream = upstreamClient.GetStream();

                    await ReadHeadersAsync(upstreamStream);

                    var interim = Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");
                    await upstreamStream.WriteAsync(interim, upstreamCts.Token);
                    await upstreamStream.FlushAsync(upstreamCts.Token);

                    await DrainChunkedBodyAsync(upstreamStream);

                    var response = Encoding.ASCII.GetBytes(
                        "HTTP/1.1 200 OK\r\n" +
                        "Content-Length: 2\r\n" +
                        "Connection: close\r\n" +
                        "\r\n" +
                        "OK");
                    await upstreamStream.WriteAsync(response, upstreamCts.Token);
                    await upstreamStream.FlushAsync(upstreamCts.Token);
                }
                catch {
                    // teardown
                }
            });

            var setting = FluxzySetting.CreateLocalRandomPort();
            await using var proxy = new Proxy(setting);

            var (clientStream, tcpClient) = await ConnectRawClient(proxy);

            await SendExpectContinueRequest(clientStream, upstreamPort);

            var interimReceived = await ReadInterimResponse(clientStream, TimeSpan.FromSeconds(8));
            Assert.Contains("HTTP/1.1 100", interimReceived);

            var body = Encoding.ASCII.GetBytes("5\r\nhello\r\n0\r\n\r\n");
            await clientStream.WriteAsync(body);
            await clientStream.FlushAsync();

            var finalReceived = await ReadAllAsync(clientStream, TimeSpan.FromSeconds(8));

            upstreamCts.Cancel();
            upstream.Stop();
            try { await upstreamTask; } catch { /* teardown */ }
            tcpClient.Dispose();

            Assert.Contains("HTTP/1.1 200", finalReceived);
            Assert.Contains("OK", finalReceived);
        }

        private static async Task<(NetworkStream stream, TcpClient client)> ConnectRawClient(Proxy proxy)
        {
            var proxyEndpoint = proxy.Run().First();
            var proxyAddress = proxyEndpoint.Address.Equals(IPAddress.Any)
                ? IPAddress.Loopback
                : proxyEndpoint.Address;

            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(proxyAddress, proxyEndpoint.Port);
            return (tcpClient.GetStream(), tcpClient);
        }

        private static async Task SendExpectContinueRequest(NetworkStream clientStream, int upstreamPort)
        {
            var request =
                $"POST http://127.0.0.1:{upstreamPort}/upload HTTP/1.1\r\n" +
                $"Host: 127.0.0.1:{upstreamPort}\r\n" +
                "Expect: 100-continue\r\n" +
                "Transfer-Encoding: chunked\r\n" +
                "Content-Type: application/octet-stream\r\n" +
                "\r\n";

            var requestBytes = Encoding.ASCII.GetBytes(request);
            await clientStream.WriteAsync(requestBytes);
            await clientStream.FlushAsync();
        }

        private static async Task<string> ReadInterimResponse(NetworkStream clientStream, TimeSpan timeout)
        {
            using var readCts = new CancellationTokenSource(timeout);
            var buffer = new byte[1024];

            try {
                var read = await clientStream.ReadAsync(buffer, readCts.Token);
                return Encoding.ASCII.GetString(buffer, 0, read);
            }
            catch (OperationCanceledException) {
                Assert.Fail(
                    "Issue #624: the proxy did not forward or synthesise an " +
                    "interim response within the read timeout — the client " +
                    "would hang waiting for 100 Continue.");
                return string.Empty;
            }
        }

        private static async Task<string> ReadAllAsync(NetworkStream clientStream, TimeSpan timeout)
        {
            using var readCts = new CancellationTokenSource(timeout);
            var sb = new StringBuilder();
            var buffer = new byte[1024];

            try {
                while (true) {
                    var read = await clientStream.ReadAsync(buffer, readCts.Token);
                    if (read == 0) break;
                    sb.Append(Encoding.ASCII.GetString(buffer, 0, read));
                    if (sb.ToString().Contains("\r\n\r\nOK", StringComparison.Ordinal)) break;
                }
            }
            catch (OperationCanceledException) {
                // acceptable — caller asserts on what was collected so far
            }

            return sb.ToString();
        }

        private static async Task ReadHeadersAsync(NetworkStream stream)
        {
            var buffer = new byte[1];
            var matches = 0;
            while (matches < 4) {
                var read = await stream.ReadAsync(buffer);
                if (read == 0) return;
                matches = buffer[0] switch {
                    (byte)'\r' when matches % 2 == 0 => matches + 1,
                    (byte)'\n' when matches % 2 == 1 => matches + 1,
                    _ => 0
                };
            }
        }

        private static async Task DrainChunkedBodyAsync(NetworkStream stream)
        {
            var sb = new StringBuilder();
            var buffer = new byte[1];
            while (true) {
                var read = await stream.ReadAsync(buffer);
                if (read == 0) return;
                sb.Append((char) buffer[0]);
                if (sb.ToString().EndsWith("0\r\n\r\n", StringComparison.Ordinal))
                    return;
            }
        }
    }
}
