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
using Fluxzy;
using Fluxzy.Certificates;
using Fluxzy.Core;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Core
{
    public class UpstreamSocketConfigurationTests
    {
        [Fact]
        public async Task Callback_Receives_Unconnected_Socket_And_Resolved_Endpoint()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint) listener.LocalEndpoint).Port;
            var acceptTask = listener.AcceptTcpClientAsync();

            UpstreamSocketContext? captured = null;
            var connectedAtCallback = true;

            var options = new UpstreamConnectOptions("example.com", port, ctx => {
                captured = ctx;
                connectedAtCallback = ctx.Socket.Connected;
            });

            await using var connection = ITcpConnectionProvider.Default.Create(string.Empty);
            var result = await connection.ConnectAsync(IPAddress.Loopback, port, options);

            Assert.NotNull(captured);
            Assert.False(connectedAtCallback);
            Assert.Equal("example.com", captured!.RequestedHost);
            Assert.Equal(port, captured.RequestedPort);
            Assert.Equal(port, captured.RemoteEndPoint.Port);
            Assert.Equal(IPAddress.Loopback, captured.RemoteEndPoint.Address);
            Assert.Equal(AddressFamily.InterNetwork, captured.Socket.AddressFamily);

            await result.Stream.DisposeAsync();
            (await acceptTask).Dispose();
            listener.Stop();
        }

        [Fact]
        public async Task Throwing_Callback_Surfaces_As_ClientError()
        {
            var options = new UpstreamConnectOptions("example.com", 443,
                _ => throw new InvalidOperationException("boom"));

            await using var connection = ITcpConnectionProvider.Default.Create(string.Empty);

            var error = await Assert.ThrowsAsync<ClientErrorException>(
                () => connection.ConnectAsync(IPAddress.Loopback, 65000, options));

            Assert.Equal(NetworkErrorCodes.UpstreamConfigurationFailed, error.ClientError.NetworkErrorCode);
        }

        [Fact]
        public async Task Proxy_Invokes_Callback_On_Upstream_Connect()
        {
            await using var origin = new LocalHttpOrigin();

            UpstreamSocketContext? captured = null;
            var connectedAtCallback = true;
            var count = 0;

            var setting = FluxzySetting.CreateLocalRandomPort();

            await using var proxy = new Proxy(setting, ctx => {
                Interlocked.Increment(ref count);
                captured = ctx;
                connectedAtCallback = ctx.Socket.Connected;
            });

            var endpoints = proxy.Run();

            using var httpClient = CreateProxiedClient(endpoints);
            using var response = await httpClient.GetAsync($"http://127.0.0.1:{origin.Port}/");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("OK", body);
            Assert.Equal(1, count);
            Assert.NotNull(captured);
            Assert.False(connectedAtCallback);
            Assert.Equal("127.0.0.1", captured!.RequestedHost);
            Assert.Equal(origin.Port, captured.RequestedPort);
            Assert.Equal(origin.Port, captured.RemoteEndPoint.Port);
            Assert.Equal(IPAddress.Loopback, captured.RemoteEndPoint.Address);
        }

        [Fact]
        public async Task Proxy_Surfaces_Throwing_Callback_As_528()
        {
            await using var origin = new LocalHttpOrigin();

            var setting = FluxzySetting.CreateLocalRandomPort();

            await using var proxy = new Proxy(setting,
                _ => throw new InvalidOperationException("boom"));

            var endpoints = proxy.Run();

            using var httpClient = CreateProxiedClient(endpoints);
            using var response = await httpClient.GetAsync($"http://127.0.0.1:{origin.Port}/");
            _ = await response.Content.ReadAsStringAsync();

            Assert.Equal(528, (int) response.StatusCode);
            Assert.True(response.Headers.TryGetValues("x-fluxzy-network-error", out var values));
            Assert.Equal(NetworkErrorCodes.UpstreamConfigurationFailed, values!.Single(),
                StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Full_Constructor_Overload_Invokes_Callback()
        {
            await using var origin = new LocalHttpOrigin();

            var count = 0;
            var setting = FluxzySetting.CreateLocalRandomPort();

            await using var proxy = new Proxy(setting,
                new CertificateProvider(setting.CaCertificate, new InMemoryCertificateCache()),
                new DefaultCertificateAuthorityManager(),
                _ => Interlocked.Increment(ref count));

            var endpoints = proxy.Run();

            using var httpClient = CreateProxiedClient(endpoints);
            using var response = await httpClient.GetAsync($"http://127.0.0.1:{origin.Port}/");
            _ = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, count);
        }

        private static HttpClient CreateProxiedClient(IReadOnlyCollection<IPEndPoint> endpoints)
        {
            return new HttpClient(new HttpClientHandler {
                Proxy = new WebProxy($"http://127.0.0.1:{endpoints.First().Port}"),
                UseProxy = true
            });
        }

        private sealed class LocalHttpOrigin : IAsyncDisposable
        {
            private readonly TcpListener _listener;
            private readonly CancellationTokenSource _cts = new();
            private readonly Task _loop;

            public LocalHttpOrigin()
            {
                _listener = new TcpListener(IPAddress.Loopback, 0);
                _listener.Start();
                Port = ((IPEndPoint) _listener.LocalEndpoint).Port;
                _loop = AcceptLoop();
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
                                var payload = Encoding.ASCII.GetBytes("OK");
                                var header = Encoding.ASCII.GetBytes(
                                    $"HTTP/1.1 200 OK\r\nContent-Length: {payload.Length}\r\n\r\n");

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
