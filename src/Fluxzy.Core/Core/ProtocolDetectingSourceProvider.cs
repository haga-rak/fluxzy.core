// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Core.Socks5;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Core
{
    /// <summary>
    /// Detects the incoming protocol by peeking the first byte and delegates
    /// to the appropriate source provider.
    /// - First byte 0x05 = SOCKS5 protocol
    /// - Otherwise = HTTP protocol (existing behavior)
    /// </summary>
    internal class ProtocolDetectingSourceProvider : ExchangeSourceProvider
    {
        private readonly FromProxyConnectSourceProvider _httpProvider;
        private readonly Socks5SourceProvider _socks5Provider;

        public ProtocolDetectingSourceProvider(
            SecureConnectionUpdater secureConnectionUpdater,
            IIdProvider idProvider,
            ProxyAuthenticationMethod proxyAuthenticationMethod,
            IExchangeContextBuilder contextBuilder)
            : base(idProvider)
        {
            _httpProvider = new FromProxyConnectSourceProvider(
                secureConnectionUpdater,
                idProvider,
                proxyAuthenticationMethod,
                contextBuilder);

            _socks5Provider = new Socks5SourceProvider(
                secureConnectionUpdater,
                idProvider,
                proxyAuthenticationMethod,
                contextBuilder);
        }

        public override async ValueTask<ExchangeSourceInitResult?> InitClientConnection(
            Stream stream,
            RsBuffer buffer,
            IPEndPoint localEndpoint,
            IPEndPoint remoteEndPoint,
            CancellationToken token)
        {
            // Peek the first byte to determine protocol
            var firstByte = new byte[1];
            var read = await stream.ReadAsync(firstByte, token).ConfigureAwait(false);

            if (read == 0)
                return null; // Connection closed

            // Create a recomposed stream that prepends the peeked byte for reading
            // but keeps the original stream for writing
            var recomposedStream = new RecomposedStream(
                new CombinedReadonlyStream(false, firstByte, stream),
                stream);

            if (firstByte[0] == Socks5Constants.Version)
            {
                // SOCKS5 protocol detected
                return await _socks5Provider.InitClientConnection(
                    recomposedStream, buffer, localEndpoint, remoteEndPoint, token).ConfigureAwait(false);
            }

            // HTTP protocol (default)
            return await _httpProvider.InitClientConnection(
                recomposedStream, buffer, localEndpoint, remoteEndPoint, token).ConfigureAwait(false);
        }
    }
}
