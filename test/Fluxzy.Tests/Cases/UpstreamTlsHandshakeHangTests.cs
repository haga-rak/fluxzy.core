// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Core;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    /// <summary>
    ///     Regression tests for upstream hosts that accept the TCP connection but never
    ///     answer the TLS ClientHello (haga-rak/fluxzy.core#634). Before the fix, the
    ///     handshake read parked forever while holding the per-authority pool creation
    ///     lock, wedging every further exchange to the same authority.
    /// </summary>
    public class UpstreamTlsHandshakeHangTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Handshake_Tarpit_Times_Out_With_Connection_Timeout(bool useBouncyCastle)
        {
            await using var server = HandshakeTarpitServer.Start();

            var setting = FluxzySetting.CreateLocalRandomPort();

            if (useBouncyCastle)
                setting.UseBouncyCastleSslEngine();

            setting.SetConnectionTimeout(TimeSpan.FromSeconds(2));

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);
            client.Timeout = TimeSpan.FromSeconds(40);

            var watch = Stopwatch.StartNew();

            using var response = await client.GetAsync($"https://local.fluxzy.io:{server.Port}/");

            watch.Stop();

            Assert.Equal(528, (int) response.StatusCode);

            Assert.True(response.Headers.TryGetValues("x-fluxzy-network-error", out var errorCodes));
            Assert.Equal(NetworkErrorCodes.ConnectionTimeout, errorCodes!.First());

            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(20),
                $"Timeout was configured at 2s but the response took {watch.Elapsed}");

            Assert.True(await server.WaitForTeardown(TimeSpan.FromSeconds(10)),
                "The hung upstream connection should be aborted when the timeout fires");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Client_Abort_Tears_Down_Hung_Handshake(bool useBouncyCastle)
        {
            await using var server = HandshakeTarpitServer.Start();

            var setting = FluxzySetting.CreateLocalRandomPort();

            if (useBouncyCastle)
                setting.UseBouncyCastleSslEngine();

            setting.SetConnectionTimeout(TimeSpan.FromSeconds(120));

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);
            client.Timeout = TimeSpan.FromSeconds(2);

            await Assert.ThrowsAsync<TaskCanceledException>(
                () => client.GetAsync($"https://local.fluxzy.io:{server.Port}/"));

            Assert.True(await server.WaitForTeardown(TimeSpan.FromSeconds(10)),
                "The hung handshake should be torn down shortly after the downstream " +
                "client aborts, instead of parking until the connection timeout");
        }

        /// <summary>
        ///     Accepts connections, reads and discards everything, never writes a byte.
        ///     Signals when its first connection is torn down.
        /// </summary>
        private sealed class HandshakeTarpitServer : IAsyncDisposable
        {
            private readonly TcpListener _listener;
            private readonly CancellationTokenSource _cts = new();
            private readonly Task _acceptLoop;

            private readonly TaskCompletionSource<bool> _tornDown =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            private HandshakeTarpitServer()
            {
                _listener = new TcpListener(IPAddress.Loopback, 0);
                _listener.Start();
                Port = ((IPEndPoint) _listener.LocalEndpoint).Port;
                _acceptLoop = AcceptLoopAsync();
            }

            public int Port { get; }

            public static HandshakeTarpitServer Start()
            {
                return new HandshakeTarpitServer();
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

                    var stream = client.GetStream();
                    var buffer = new byte[16 * 1024];

                    while (true) {
                        var read = await stream.ReadAsync(buffer, _cts.Token);

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

                _cts.Dispose();
            }
        }
    }
}
