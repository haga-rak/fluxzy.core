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
    ///     Regression tests for upstream hosts that accept the connection (and the TLS
    ///     handshake) but never send a response header (haga-rak/fluxzy.core#634).
    ///     Before the fix, the exchange parked forever on the upstream read: no response
    ///     header timeout existed, and a downstream client abort was never observed while
    ///     waiting on upstream.
    /// </summary>
    public class UpstreamResponseHeaderHangTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Tarpit_Upstream_Times_Out_With_Response_Header_Timeout(bool useBouncyCastle)
        {
            await using var server = TarpitServer.Start(useTls: true);

            var setting = FluxzySetting.CreateLocalRandomPort();

            if (useBouncyCastle)
                setting.UseBouncyCastleSslEngine();

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

            Assert.True(await server.WaitForTeardown(TimeSpan.FromSeconds(10)),
                "The hung upstream connection should be aborted when the timeout fires");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Client_Abort_Tears_Down_Hung_Upstream(bool useTls)
        {
            await using var server = TarpitServer.Start(useTls);

            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.SetResponseHeaderTimeout(TimeSpan.FromSeconds(120));

            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);
            client.Timeout = TimeSpan.FromSeconds(2);

            var url = useTls
                ? $"https://local.fluxzy.io:{server.Port}/"
                : $"http://127.0.0.1:{server.Port}/";

            await Assert.ThrowsAsync<TaskCanceledException>(() => client.GetAsync(url));

            Assert.True(await server.WaitForTeardown(TimeSpan.FromSeconds(10)),
                "The hung upstream connection should be torn down shortly after the " +
                "downstream client aborts, instead of parking until the header timeout");
        }

        [Fact]
        public async Task Slow_Response_Within_Timeout_Succeeds()
        {
            await using var server = TarpitServer.Start(useTls: true, respondAfter: TimeSpan.FromSeconds(3));

            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.SetResponseHeaderTimeout(TimeSpan.FromSeconds(30));

            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);
            client.Timeout = TimeSpan.FromSeconds(40);

            using var response = await client.GetAsync($"https://local.fluxzy.io:{server.Port}/");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("ok", body);
        }

        /// <summary>
        ///     Accepts connections (optionally completing a TLS handshake), reads the
        ///     request, then never answers unless <c>respondAfter</c> is set. Signals
        ///     when its first connection is torn down (FIN, RST or local error).
        /// </summary>
        private sealed class TarpitServer : IAsyncDisposable
        {
            private readonly TcpListener _listener;
            private readonly bool _useTls;
            private readonly TimeSpan? _respondAfter;
            private readonly CancellationTokenSource _cts = new();
            private readonly Task _acceptLoop;

            private readonly TaskCompletionSource<bool> _tornDown =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            private readonly X509Certificate2? _certificate;

            private TarpitServer(bool useTls, TimeSpan? respondAfter)
            {
                _useTls = useTls;
                _respondAfter = respondAfter;

                if (useTls) {
                    _certificate = new X509Certificate2(
                        "_Files/Certificates/client-cert.pifix",
                        CertificateContext.DefaultPassword);
                }

                _listener = new TcpListener(IPAddress.Loopback, 0);
                _listener.Start();
                Port = ((IPEndPoint) _listener.LocalEndpoint).Port;
                _acceptLoop = AcceptLoopAsync();
            }

            public int Port { get; }

            public static TarpitServer Start(bool useTls, TimeSpan? respondAfter = null)
            {
                return new TarpitServer(useTls, respondAfter);
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

                    Stream stream = client.GetStream();

                    if (_useTls) {
                        var sslStream = new SslStream(stream, false);

                        await sslStream.AuthenticateAsServerAsync(_certificate!,
                            clientCertificateRequired: false,
                            enabledSslProtocols: SslProtocols.Tls12,
                            checkCertificateRevocation: false);

                        stream = sslStream;
                    }

                    var buffer = new byte[16 * 1024];
                    var received = new StringBuilder();
                    var requestReceived = false;

                    while (true) {
                        var read = await stream.ReadAsync(buffer, _cts.Token);

                        if (read == 0)
                            return;

                        received.Append(Encoding.ASCII.GetString(buffer, 0, read));

                        if (!requestReceived &&
                            received.ToString().Contains("\r\n\r\n", StringComparison.Ordinal)) {
                            requestReceived = true;

                            if (_respondAfter != null) {
                                await Task.Delay(_respondAfter.Value, _cts.Token);

                                var response =
                                    "HTTP/1.1 200 OK\r\n" +
                                    "Content-Length: 2\r\n" +
                                    "Connection: close\r\n" +
                                    "\r\n" +
                                    "ok";

                                await stream.WriteAsync(Encoding.ASCII.GetBytes(response), _cts.Token);

                                return;
                            }
                        }

                        // Tarpit: never answer, keep draining until the peer goes away
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

                _certificate?.Dispose();
                _cts.Dispose();
            }
        }
    }
}
