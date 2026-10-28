// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Clients;
using Fluxzy.Core;
using Fluxzy.Rules;
using Fluxzy.Tests.UnitTests.Ssl;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Core
{
    /// <summary>
    ///     Issue #823: a client may send the TLS ClientHello right behind the CONNECT header without
    ///     waiting for the 200. When both land in the same read, the bytes past the header must be
    ///     replayed to the TLS handshake (or to the tunnel in blind mode) instead of being dropped.
    /// </summary>
    public class ConnectPipelinedBytesTests
    {
        private const string Target = "example.com:443";

        [Fact]
        public async Task Connect_WithPipelinedClientHello_CompletesTlsHandshake()
        {
            var result = await Run(blindMode: false, async (clientStream, token) => {
                // Prefix the first TLS write with the CONNECT header so both leave in one send,
                // and swallow the 200 before SslStream reads the ServerHello.
                var pipelined = new PipelinedConnectStream(clientStream, Target);
                var ssl = new SslStream(pipelined, false, (_, _, _, _) => true);

                await ssl.AuthenticateAsClientAsync("example.com");

                return ssl;
            });

            Assert.NotNull(result);
            Assert.True(result!.ProvisionalExchange.Authority.Secure);
            Assert.False(result.DownStreamPipe.TunnelOnly);
        }

        [Fact]
        public async Task Connect_BlindMode_WithPipelinedClientHello_ReplaysBytesToTunnel()
        {
            var clientHello = TlsClientHelloParserTests.BuildClientHello("example.com");

            var result = await Run(blindMode: true, async (clientStream, token) => {
                var connect = Encoding.ASCII.GetBytes($"CONNECT {Target} HTTP/1.1\r\nHost: {Target}\r\n\r\n");
                var single = new byte[connect.Length + clientHello.Length];

                connect.CopyTo(single, 0);
                clientHello.CopyTo(single, connect.Length);

                await clientStream.WriteAsync(single, token);

                return null;
            });

            Assert.NotNull(result);

            var (readStream, _) = result!.DownStreamPipe.AbandonPipe();
            var replayed = new byte[clientHello.Length];

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await readStream.ReadExactlyAsync(replayed, cts.Token);

            Assert.Equal(clientHello, replayed);
        }

        private static async Task<ExchangeSourceInitResult?> Run(
            bool blindMode,
            Func<Stream, CancellationToken, Task<IDisposable?>> clientAction)
        {
            var root = Certificate.UseDefault();
            using var certProvider = new CertificateProvider(root, new InMemoryCertificateCache());
            var setting = FluxzySetting.CreateLocalRandomPort();
            var updater = new SecureConnectionUpdater(certProvider, serveH2: false);

            var provider = new FromProxyConnectSourceProvider(updater, new FromIndexIdProvider(0, 0),
                NoAuthenticationMethod.Instance, new TestExchangeContextBuilder(setting, blindMode));

            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint) listener.LocalEndpoint).Port;

            // Short enough that a regression (the handshake blocking on a read) fails fast.
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            var serverTask = Task.Run(async () => {
                using var serverConnection = await listener.AcceptTcpClientAsync(cts.Token);
                using var buffer = Fluxzy.Misc.ResizableBuffers.RsBuffer.Allocate(1024 * 8);

                return await provider.InitClientConnection(
                    serverConnection.GetStream(), buffer,
                    (IPEndPoint) serverConnection.Client.LocalEndPoint!,
                    (IPEndPoint) serverConnection.Client.RemoteEndPoint!,
                    cts.Token);
            });

            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);

            IDisposable? keepAlive = null;

            try {
                keepAlive = await clientAction(client.GetStream(), cts.Token);
                return await serverTask;
            }
            finally {
                keepAlive?.Dispose();
                listener.Stop();
            }
        }

        /// <summary>
        ///     Sends the CONNECT header in the same write as the first client payload, then skips the
        ///     proxy 200 on the first read so the wrapped SslStream sees TLS records only.
        /// </summary>
        private sealed class PipelinedConnectStream : Stream
        {
            private readonly Stream _inner;
            private byte[]? _pendingConnect;
            private bool _responseSkipped;

            public PipelinedConnectStream(Stream inner, string target)
            {
                _inner = inner;
                _pendingConnect = Encoding.ASCII.GetBytes($"CONNECT {target} HTTP/1.1\r\nHost: {target}\r\n\r\n");
            }

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => throw new NotSupportedException();

            public override long Position {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default)
            {
                if (_pendingConnect != null) {
                    var combined = new byte[_pendingConnect.Length + buffer.Length];
                    _pendingConnect.CopyTo(combined, 0);
                    buffer.CopyTo(combined.AsMemory(_pendingConnect.Length));
                    _pendingConnect = null;

                    await _inner.WriteAsync(combined, token);
                    return;
                }

                await _inner.WriteAsync(buffer, token);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
            {
                return WriteAsync(buffer.AsMemory(offset, count), token).AsTask();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                WriteAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
            {
                if (!_responseSkipped) {
                    await SkipProxyResponse(token);
                    _responseSkipped = true;
                }

                return await _inner.ReadAsync(buffer, token);
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
            {
                return ReadAsync(buffer.AsMemory(offset, count), token).AsTask();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
            }

            private async Task SkipProxyResponse(CancellationToken token)
            {
                var single = new byte[1];
                var matched = 0;

                while (matched < 4) {
                    var read = await _inner.ReadAsync(single, token);

                    if (read == 0)
                        throw new EndOfStreamException("Proxy closed before answering CONNECT");

                    var expected = matched % 2 == 0 ? (byte) '\r' : (byte) '\n';
                    matched = single[0] == expected ? matched + 1 : single[0] == '\r' ? 1 : 0;
                }
            }

            public override void Flush() => _inner.Flush();

            public override Task FlushAsync(CancellationToken token) => _inner.FlushAsync(token);

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();
        }

        private sealed class TestExchangeContextBuilder : IExchangeContextBuilder
        {
            private readonly FluxzySetting _setting;
            private readonly bool _blindMode;
            private readonly VariableContext _variableContext = new();

            public TestExchangeContextBuilder(FluxzySetting setting, bool blindMode)
            {
                _setting = setting;
                _blindMode = blindMode;
            }

            public ValueTask<ExchangeContext> Create(Authority authority, bool secure)
            {
                return new ValueTask<ExchangeContext>(
                    new ExchangeContext(authority, _variableContext, _setting, null!) {
                        Secure = secure,
                        BlindMode = _blindMode
                    });
            }
        }
    }
}
