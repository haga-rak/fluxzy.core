// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.Ssl;
using Fluxzy.Core;
using Fluxzy.Misc.Streams;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Core
{
    public class RemoteConnectionBuilderTests
    {
        private static readonly List<SslApplicationProtocol> HttpProtocols = new() {
            SslApplicationProtocol.Http2,
            SslApplicationProtocol.Http11
        };

        [Fact]
        public async Task TlsFailureDisposesOpenedStreamAndKeepsConnection()
        {
            var opened = await OpenConnectedStream();
            using var peer = opened.Peer;
            var expected = new InvalidDataException("TLS failed");
            var builder = CreateBuilder((_, _, _, _) => Task.FromException<SslConnection>(expected));
            var exchange = CreateExchange();

            var actual = await Assert.ThrowsAsync<InvalidDataException>(() =>
                builder.OpenConnectionToRemote(
                    exchange, Resolution(), HttpProtocols, Setting(opened.Stream), null,
                    CancellationToken.None).AsTask());

            Assert.Same(expected, actual);
            Assert.Equal(1, opened.DisposeCount());
            Assert.NotNull(exchange.Connection);
        }

        [Fact]
        public async Task UpstreamConnectFailureDisposesOpenedStreamAndKeepsConnection()
        {
            var opened = await OpenConnectedStream();
            using var peer = opened.Peer;
            var proxyResponse = ReplyToConnect(opened.Peer);
            var builder = CreateBuilder((_, _, _, _) =>
                Task.FromException<SslConnection>(new InvalidOperationException("TLS should not start")));
            var exchange = CreateExchange();

            var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                builder.OpenConnectionToRemote(
                    exchange, Resolution(), HttpProtocols, Setting(opened.Stream),
                    new ProxyConfiguration("proxy.example", 8080), CancellationToken.None).AsTask());
            await proxyResponse;

            Assert.Contains("Failed to connect to upstream proxy", error.Message);
            Assert.Equal(1, opened.DisposeCount());
            Assert.NotNull(exchange.Connection);
        }

        [Fact]
        public async Task CallerCancellationPreservesExceptionAndDisposesOpenedStream()
        {
            var opened = await OpenConnectedStream();
            using var peer = opened.Peer;
            using var cancellation = new CancellationTokenSource();
            var expected = new OperationCanceledException("caller cancelled", null, cancellation.Token);
            var builder = CreateBuilder((_, _, _, _) => {
                cancellation.Cancel();
                return Task.FromException<SslConnection>(expected);
            });
            var exchange = CreateExchange();

            var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                builder.OpenConnectionToRemote(
                    exchange, Resolution(), HttpProtocols, Setting(opened.Stream), null,
                    cancellation.Token).AsTask());

            Assert.Same(expected, actual);
            Assert.Equal(1, opened.DisposeCount());
            Assert.NotNull(exchange.Connection);
        }

        [Fact]
        public async Task ConnectionTimeoutPreservesMappingAndDisposesOpenedStream()
        {
            var opened = await OpenConnectedStream();
            using var peer = opened.Peer;
            var builder = CreateBuilder(async (_, _, _, token) => {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                throw new InvalidOperationException("unreachable");
            });
            var exchange = CreateExchange();

            var error = await Assert.ThrowsAsync<ClientErrorException>(() =>
                builder.OpenConnectionToRemote(
                    exchange, Resolution(), HttpProtocols,
                    Setting(opened.Stream, TimeSpan.FromMilliseconds(50)), null,
                    CancellationToken.None).AsTask());

            Assert.Equal(NetworkErrorCodes.ConnectionTimeout, error.ClientError.NetworkErrorCode);
            Assert.Equal(1, opened.DisposeCount());
            Assert.NotNull(exchange.Connection);
        }

        [Fact]
        public async Task FailureBeforeStreamCreationKeepsConnectionWithoutDisposal()
        {
            var expected = new SocketException((int) SocketError.ConnectionRefused);
            var provider = new TestTcpConnectionProvider(_ =>
                Task.FromException<ITcpConnectionConnectResult>(expected));
            var builder = CreateBuilder((_, _, _, _) =>
                Task.FromException<SslConnection>(new InvalidOperationException("TLS should not start")));
            var exchange = CreateExchange();

            var actual = await Assert.ThrowsAsync<SocketException>(() =>
                builder.OpenConnectionToRemote(
                    exchange, Resolution(), HttpProtocols, Setting(provider), null,
                    CancellationToken.None).AsTask());

            Assert.Same(expected, actual);
            Assert.Equal(0, provider.StreamsReturned);
            Assert.NotNull(exchange.Connection);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SuccessReturnsExactConnectionWithoutDisposingStream(bool useHttp2)
        {
            var opened = await OpenConnectedStream();
            using var peer = opened.Peer;
            var protocol = useHttp2 ? SslApplicationProtocol.Http2 : SslApplicationProtocol.Http11;
            var builder = CreateBuilder((stream, _, _, _) => Task.FromResult(
                new SslConnection(stream, CreateSslInfo(), protocol)));
            var exchange = CreateExchange();

            var result = await builder.OpenConnectionToRemote(
                exchange, Resolution(), HttpProtocols, Setting(opened.Stream), null,
                CancellationToken.None);

            Assert.Same(exchange.Connection, result.Connection);
            Assert.Same(opened.Stream, result.Connection.ReadStream);
            Assert.Equal(useHttp2 ? RemoteConnectionResultType.Http2 : RemoteConnectionResultType.Http11,
                result.Type);
            Assert.Equal(0, opened.DisposeCount());

            await opened.Stream.DisposeAsync();
        }

        private static RemoteConnectionBuilder CreateBuilder(
            Func<Stream, SslConnectionBuilderOptions, Action<string>, CancellationToken,
                Task<SslConnection>> authenticate)
        {
            return new RemoteConnectionBuilder(ITimingProvider.Default,
                new TestSslConnectionBuilder(authenticate));
        }

        private static ProxyRuntimeSetting Setting(
            DisposeEventNotifierStream stream, TimeSpan? timeout = null)
        {
            var provider = new TestTcpConnectionProvider(_ =>
                Task.FromResult<ITcpConnectionConnectResult>(new TestConnectResult(stream)));
            return Setting(provider, timeout);
        }

        private static ProxyRuntimeSetting Setting(
            ITcpConnectionProvider provider, TimeSpan? timeout = null)
        {
            var setting = ProxyRuntimeSetting.CreateDefault;
            setting.TcpConnectionProvider = provider;
            setting.ConnectionTimeout = timeout ?? Timeout.InfiniteTimeSpan;
            return setting;
        }

        private static Exchange CreateExchange()
        {
            var authority = new Authority("example.com", 443, true);
            return new Exchange(IIdProvider.FromZero, authority,
                "GET / HTTP/1.1\r\nHost: example.com\r\n\r\n".AsMemory(),
                "HTTP/1.1", DateTime.UtcNow);
        }

        private static DnsResolutionResult Resolution()
        {
            var now = DateTime.UtcNow;
            return new DnsResolutionResult(new IPEndPoint(IPAddress.Loopback, 443), now, now);
        }

        private static SslInfo CreateSslInfo()
        {
            return new SslInfo(
                SslProtocols.Tls12, null, null, null, null, string.Empty, string.Empty,
                HashAlgorithmType.None, CipherAlgorithmType.None,
                TlsCipherSuite.TLS_AES_128_GCM_SHA256,
                null, null, null, null, null, null, null, null);
        }

        private static async Task<(DisposeEventNotifierStream Stream, TcpClient Peer,
            Func<int> DisposeCount)> OpenConnectedStream()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            var source = new TcpClient(AddressFamily.InterNetwork);
            var port = ((IPEndPoint) listener.LocalEndpoint).Port;
            var connect = source.ConnectAsync(IPAddress.Loopback, port);
            var peer = await listener.AcceptTcpClientAsync();
            await connect;

            var disposeCount = 0;
            var stream = new DisposeEventNotifierStream(source, () => {
                Interlocked.Increment(ref disposeCount);
                return ValueTask.CompletedTask;
            });

            return (stream, peer, () => Volatile.Read(ref disposeCount));
        }

        private static async Task ReplyToConnect(TcpClient peer)
        {
            var stream = peer.GetStream();
            var request = new byte[1024];
            var totalRead = 0;

            while (totalRead < request.Length) {
                var read = await stream.ReadAsync(request.AsMemory(totalRead));
                if (read == 0)
                    break;

                totalRead += read;
                if (Encoding.ASCII.GetString(request, 0, totalRead).Contains("\r\n\r\n"))
                    break;
            }

            Assert.Contains("CONNECT example.com:443 HTTP/1.1",
                Encoding.ASCII.GetString(request, 0, totalRead));
            await stream.WriteAsync("HTTP/1.1 407 Proxy Authentication Required\r\n\r\n"u8.ToArray());
        }

        private sealed class TestSslConnectionBuilder : ISslConnectionBuilder
        {
            private readonly Func<Stream, SslConnectionBuilderOptions, Action<string>, CancellationToken,
                Task<SslConnection>> _authenticate;

            public TestSslConnectionBuilder(
                Func<Stream, SslConnectionBuilderOptions, Action<string>, CancellationToken,
                    Task<SslConnection>> authenticate)
            {
                _authenticate = authenticate;
            }

            public Task<SslConnection> AuthenticateAsClient(
                Stream innerStream, SslConnectionBuilderOptions sslOptions,
                Action<string> onKeyReceived, CancellationToken token)
            {
                return _authenticate(innerStream, sslOptions, onKeyReceived, token);
            }
        }

        private sealed class TestTcpConnectionProvider : ITcpConnectionProvider
        {
            private readonly Func<CancellationToken, Task<ITcpConnectionConnectResult>> _connect;

            public TestTcpConnectionProvider(
                Func<CancellationToken, Task<ITcpConnectionConnectResult>> connect)
            {
                _connect = connect;
            }

            public int StreamsReturned { get; private set; }

            public ITcpConnection Create(string dumpFileName)
            {
                return new TestTcpConnection(async token => {
                    var result = await _connect(token);
                    StreamsReturned++;
                    return result;
                });
            }

            public void TryFlush()
            {
            }

            public ValueTask DisposeAsync()
            {
                return default;
            }
        }

        private sealed class TestTcpConnection : ITcpConnection
        {
            private readonly Func<CancellationToken, Task<ITcpConnectionConnectResult>> _connect;

            public TestTcpConnection(
                Func<CancellationToken, Task<ITcpConnectionConnectResult>> connect)
            {
                _connect = connect;
            }

            public Task<ITcpConnectionConnectResult> ConnectAsync(IPAddress address, int port)
            {
                return _connect(CancellationToken.None);
            }

            public Task<ITcpConnectionConnectResult> ConnectAsync(
                IPAddress address, int port, UpstreamConnectOptions options, CancellationToken token)
            {
                return _connect(token);
            }

            public ValueTask DisposeAsync()
            {
                return default;
            }
        }

        private sealed class TestConnectResult : ITcpConnectionConnectResult
        {
            public TestConnectResult(DisposeEventNotifierStream stream)
            {
                Stream = stream;
            }

            public DisposeEventNotifierStream Stream { get; }

            public void ProcessNssKey(string nssKey)
            {
            }
        }
    }
}
