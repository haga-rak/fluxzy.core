// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Rules.Actions;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    /// <summary>
    ///     Follow-up coverage for haga-rak/fluxzy.core#634: the response header timeout on
    ///     HTTP/2 upstreams and the opt-in response body idle timeout on both protocols.
    /// </summary>
    public class UpstreamStallTests
    {
        [Fact]
        public async Task H2_Tarpit_Times_Out_With_Response_Header_Timeout()
        {
            await using var server = H2StallServer.Start(sendPartialBodyOnRequest: false);

            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.SetResponseHeaderTimeout(TimeSpan.FromSeconds(2));

            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);
            client.Timeout = TimeSpan.FromSeconds(40);

            var watch = Stopwatch.StartNew();

            using var response = await client.GetAsync($"https://local.fluxzy.io:{server.Port}/");

            watch.Stop();

            Assert.Equal(528, (int) response.StatusCode);

            Assert.True(response.Headers.TryGetValues("x-fluxzy-network-error", out var errorCodes));
            Assert.Equal(NetworkErrorCodes.ResponseHeaderTimeout, errorCodes!.First());

            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(20),
                $"Timeout was configured at 2s but the response took {watch.Elapsed}");

            Assert.True(await server.WaitForRstStream(TimeSpan.FromSeconds(10)),
                "The timed out stream should be reset instead of leaving the server waiting");
        }

        [Fact]
        public async Task H2_Stalled_Response_Body_Aborted_By_Idle_Timeout()
        {
            await using var server = H2StallServer.Start(sendPartialBodyOnRequest: true);

            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.SetResponseBodyIdleTimeout(TimeSpan.FromSeconds(2));

            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);
            client.Timeout = TimeSpan.FromSeconds(40);

            var watch = Stopwatch.StartNew();

            await Assert.ThrowsAnyAsync<HttpRequestException>(async () => {
                using var response = await client.GetAsync($"https://local.fluxzy.io:{server.Port}/");
                await response.Content.ReadAsStringAsync();
            });

            watch.Stop();

            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(20),
                $"Idle timeout was configured at 2s but the failure took {watch.Elapsed}");

            Assert.True(await server.WaitForRstStream(TimeSpan.FromSeconds(10)),
                "The stalled stream should be reset");
        }

        [Fact]
        public async Task H1_Stalled_Response_Body_Aborted_By_Idle_Timeout()
        {
            await using var server = H1BodyStallServer.Start(dripDelay: null);

            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.SetResponseBodyIdleTimeout(TimeSpan.FromSeconds(2));

            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);
            client.Timeout = TimeSpan.FromSeconds(40);

            var watch = Stopwatch.StartNew();

            await Assert.ThrowsAnyAsync<HttpRequestException>(async () => {
                using var response = await client.GetAsync($"https://local.fluxzy.io:{server.Port}/");
                await response.Content.ReadAsStringAsync();
            });

            watch.Stop();

            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(20),
                $"Idle timeout was configured at 2s but the failure took {watch.Elapsed}");

            Assert.True(await server.WaitForTeardown(TimeSpan.FromSeconds(10)),
                "The stalled upstream connection should be aborted");
        }

        [Fact]
        public async Task H1_Slow_Drip_Body_Within_Idle_Timeout_Succeeds()
        {
            await using var server = H1BodyStallServer.Start(dripDelay: TimeSpan.FromMilliseconds(400));

            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.SetResponseBodyIdleTimeout(TimeSpan.FromSeconds(3));

            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);
            client.Timeout = TimeSpan.FromSeconds(40);

            using var response = await client.GetAsync($"https://local.fluxzy.io:{server.Port}/");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal("abcdef", body);
        }

        /// <summary>
        ///     HTTP/1.1 over TLS origin that answers with Content-Length: 6 and either
        ///     drips one byte per <c>dripDelay</c> (completing normally) or sends 2 bytes
        ///     and stalls forever.
        /// </summary>
        private sealed class H1BodyStallServer : IAsyncDisposable
        {
            private readonly TcpListener _listener;
            private readonly TimeSpan? _dripDelay;
            private readonly CancellationTokenSource _cts = new();
            private readonly Task _acceptLoop;
            private readonly X509Certificate2 _certificate;

            private readonly TaskCompletionSource<bool> _tornDown =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            private H1BodyStallServer(TimeSpan? dripDelay)
            {
                _dripDelay = dripDelay;

                _certificate = new X509Certificate2(
                    "_Files/Certificates/client-cert.pifix",
                    CertificateContext.DefaultPassword);

                _listener = new TcpListener(IPAddress.Loopback, 0);
                _listener.Start();
                Port = ((IPEndPoint) _listener.LocalEndpoint).Port;
                _acceptLoop = AcceptLoopAsync();
            }

            public int Port { get; }

            public static H1BodyStallServer Start(TimeSpan? dripDelay)
            {
                return new H1BodyStallServer(dripDelay);
            }

            public async Task<bool> WaitForTeardown(TimeSpan timeout)
            {
                var completed = await Task.WhenAny(_tornDown.Task, Task.Delay(timeout, _cts.Token));

                return completed == _tornDown.Task;
            }

            private async Task AcceptLoopAsync()
            {
                try {
                    while (!_cts.IsCancellationRequested) {
                        var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                        _ = HandleClientAsync(client);
                    }
                }
                catch {
                    // listener stopped
                }
            }

            private async Task HandleClientAsync(TcpClient client)
            {
                try {
                    using var _ = client;

                    await using var sslStream = new SslStream(client.GetStream(), false);

                    await sslStream.AuthenticateAsServerAsync(_certificate,
                        clientCertificateRequired: false,
                        enabledSslProtocols: SslProtocols.Tls12,
                        checkCertificateRevocation: false);

                    var buffer = new byte[16 * 1024];
                    var received = new StringBuilder();

                    while (!received.ToString().Contains("\r\n\r\n", StringComparison.Ordinal)) {
                        var read = await sslStream.ReadAsync(buffer, _cts.Token);

                        if (read == 0)
                            return;

                        received.Append(Encoding.ASCII.GetString(buffer, 0, read));
                    }

                    var body = "abcdef"u8.ToArray();

                    var header =
                        "HTTP/1.1 200 OK\r\n" +
                        $"Content-Length: {body.Length}\r\n" +
                        "Connection: keep-alive\r\n" +
                        "\r\n";

                    await sslStream.WriteAsync(Encoding.ASCII.GetBytes(header), _cts.Token);

                    if (_dripDelay == null) {
                        await sslStream.WriteAsync(body.AsMemory(0, 2), _cts.Token);
                        await sslStream.FlushAsync(_cts.Token);

                        // Stall: never send the remaining bytes, wait for the peer to go away
                        while (true) {
                            var read = await sslStream.ReadAsync(buffer, _cts.Token);

                            if (read == 0)
                                return;
                        }
                    }

                    foreach (var b in body) {
                        await Task.Delay(_dripDelay.Value, _cts.Token);
                        await sslStream.WriteAsync(new[] { b }, _cts.Token);
                        await sslStream.FlushAsync(_cts.Token);
                    }

                    while (true) {
                        var read = await sslStream.ReadAsync(buffer, _cts.Token);

                        if (read == 0)
                            return;
                    }
                }
                catch {
                    // FIN/RST/abort observed
                }
                finally {
                    _tornDown.TrySetResult(true);
                }
            }

            public async ValueTask DisposeAsync()
            {
                _cts.Cancel();
                _listener.Stop();

                try {
                    await _acceptLoop;
                }
                catch {
                }

                _certificate.Dispose();
                _cts.Dispose();
            }
        }

        /// <summary>
        ///     Minimal raw HTTP/2 origin (TLS, ALPN h2). Completes the SETTINGS exchange,
        ///     then either ignores request HEADERS forever (tarpit) or answers with
        ///     `:status: 200` plus a partial DATA frame and stalls. Records whether an
        ///     RST_STREAM ever arrives.
        /// </summary>
        private sealed class H2StallServer : IAsyncDisposable
        {
            private readonly TcpListener _listener;
            private readonly bool _sendPartialBodyOnRequest;
            private readonly CancellationTokenSource _cts = new();
            private readonly Task _acceptLoop;
            private readonly X509Certificate2 _certificate;

            private readonly TaskCompletionSource<bool> _rstReceived =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            private H2StallServer(bool sendPartialBodyOnRequest)
            {
                _sendPartialBodyOnRequest = sendPartialBodyOnRequest;

                _certificate = new X509Certificate2(
                    "_Files/Certificates/client-cert.pifix",
                    CertificateContext.DefaultPassword);

                _listener = new TcpListener(IPAddress.Loopback, 0);
                _listener.Start();
                Port = ((IPEndPoint) _listener.LocalEndpoint).Port;
                _acceptLoop = AcceptLoopAsync();
            }

            public int Port { get; }

            public static H2StallServer Start(bool sendPartialBodyOnRequest)
            {
                return new H2StallServer(sendPartialBodyOnRequest);
            }

            public async Task<bool> WaitForRstStream(TimeSpan timeout)
            {
                var completed = await Task.WhenAny(_rstReceived.Task, Task.Delay(timeout, _cts.Token));

                return completed == _rstReceived.Task;
            }

            private async Task AcceptLoopAsync()
            {
                try {
                    while (!_cts.IsCancellationRequested) {
                        var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                        _ = HandleClientAsync(client);
                    }
                }
                catch {
                    // listener stopped
                }
            }

            private async Task HandleClientAsync(TcpClient client)
            {
                try {
                    using var _ = client;

                    await using var sslStream = new SslStream(client.GetStream(), false);

                    await sslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions {
                        ServerCertificate = _certificate,
                        ApplicationProtocols = new() { SslApplicationProtocol.Http2 },
                        EnabledSslProtocols = SslProtocols.Tls12,
                        ClientCertificateRequired = false
                    }, _cts.Token);

                    // Client connection preface (24 bytes) before any frame
                    await ReadExactAsync(sslStream, new byte[24]);

                    // Server preface: empty SETTINGS
                    await WriteFrameAsync(sslStream, type: 0x04, flags: 0, streamId: 0,
                        Array.Empty<byte>());

                    while (true) {
                        var (type, flags, streamId, payload) = await ReadFrameAsync(sslStream);

                        if (type == 0x04 && (flags & 0x01) == 0) {
                            // SETTINGS from client: acknowledge
                            await WriteFrameAsync(sslStream, type: 0x04, flags: 0x01, streamId: 0,
                                Array.Empty<byte>());
                        }
                        else if (type == 0x06 && (flags & 0x01) == 0) {
                            // PING: acknowledge with same payload
                            await WriteFrameAsync(sslStream, type: 0x06, flags: 0x01, streamId: 0, payload);
                        }
                        else if (type == 0x01 && _sendPartialBodyOnRequest) {
                            // Request HEADERS: answer `:status: 200` (HPACK static index 8)
                            // without END_STREAM, then a partial DATA frame, then stall.
                            await WriteFrameAsync(sslStream, type: 0x01, flags: 0x04, streamId,
                                new byte[] { 0x88 });

                            await WriteFrameAsync(sslStream, type: 0x00, flags: 0, streamId,
                                "partial-"u8.ToArray());
                        }
                        else if (type == 0x03) {
                            _rstReceived.TrySetResult(true);
                        }

                        // Everything else (WINDOW_UPDATE, HEADERS in tarpit mode…) is ignored
                    }
                }
                catch {
                    // Connection torn down
                }
            }

            private static async Task<(byte Type, byte Flags, int StreamId, byte[] Payload)>
                ReadFrameAsync(Stream stream)
            {
                var header = new byte[9];
                await ReadExactAsync(stream, header);

                var length = (header[0] << 16) | (header[1] << 8) | header[2];
                var type = header[3];
                var flags = header[4];
                var streamId = ((header[5] & 0x7F) << 24) | (header[6] << 16) | (header[7] << 8) | header[8];

                var payload = new byte[length];
                await ReadExactAsync(stream, payload);

                return (type, flags, streamId, payload);
            }

            private static async Task ReadExactAsync(Stream stream, byte[] buffer)
            {
                var total = 0;

                while (total < buffer.Length) {
                    var read = await stream.ReadAsync(buffer.AsMemory(total));

                    if (read == 0)
                        throw new IOException("Peer closed the connection");

                    total += read;
                }
            }

            private static async Task WriteFrameAsync(
                Stream stream, byte type, byte flags, int streamId, byte[] payload)
            {
                var frame = new byte[9 + payload.Length];

                frame[0] = (byte) ((payload.Length >> 16) & 0xFF);
                frame[1] = (byte) ((payload.Length >> 8) & 0xFF);
                frame[2] = (byte) (payload.Length & 0xFF);
                frame[3] = type;
                frame[4] = flags;
                frame[5] = (byte) ((streamId >> 24) & 0x7F);
                frame[6] = (byte) ((streamId >> 16) & 0xFF);
                frame[7] = (byte) ((streamId >> 8) & 0xFF);
                frame[8] = (byte) (streamId & 0xFF);

                payload.CopyTo(frame, 9);

                await stream.WriteAsync(frame);
                await stream.FlushAsync();
            }

            public async ValueTask DisposeAsync()
            {
                _cts.Cancel();
                _listener.Stop();

                try {
                    await _acceptLoop;
                }
                catch {
                }

                _certificate.Dispose();
                _cts.Dispose();
            }
        }
    }
}
