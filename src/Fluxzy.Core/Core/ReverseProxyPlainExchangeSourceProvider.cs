// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.H11;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Core
{
    internal class ReverseProxyPlainExchangeSourceProvider : ExchangeSourceProvider
    {
        private readonly IIdProvider _idProvider;
        private readonly int? _reverseModeForcedPort;

        public ReverseProxyPlainExchangeSourceProvider(IIdProvider idProvider,
            int? reverseModeForcedPort)
            : base(idProvider)
        {
            _idProvider = idProvider;
            _reverseModeForcedPort = reverseModeForcedPort;
        }

        public override async ValueTask<ExchangeSourceInitResult?> InitClientConnection(
            Stream stream,
            RsBuffer buffer,
            IExchangeContextBuilder contextBuilder,
            IPEndPoint localEndpoint, IPEndPoint remoteEndPoint,
            CancellationToken token)
        {
            var receivedFromProxy = ITimingProvider.Default.Instant();

            var blockReadResult = await
                Http11HeaderBlockReader.GetNext(stream, 
                    buffer, null, null, throwOnError: true, token).ConfigureAwait(false);

            if (blockReadResult.TotalReadLength == 0)
                return null; // NO read here 

            var plainHeaderChars = new char[blockReadResult.HeaderLength];

            Encoding.ASCII.GetChars(new Memory<byte>(buffer.Buffer, 0, blockReadResult.HeaderLength).Span,
                plainHeaderChars);

            var plainHeader = new RequestHeader(plainHeaderChars, false);
            var remainder = blockReadResult.TotalReadLength - blockReadResult.HeaderLength;

            var plainStream = stream;

            if (remainder > 0)
            {
                var extraBlock = new byte[remainder];

                buffer.Buffer.AsSpan(blockReadResult.HeaderLength, remainder)
                      .CopyTo(extraBlock);

                plainStream = new RecomposedStream(
                    new CombinedReadonlyStream(true, new MemoryStream(extraBlock), plainStream),
                    plainStream);
            }

            // Plain request 

            var path = plainHeader.Path.ToString();

            if (!Uri.TryCreate(path, UriKind.Absolute, out var uri)
                || !uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var builder = new StringBuilder("http://");

                builder.Append(plainHeader.Authority.Span);

                if (!path.StartsWith("/"))
                    builder.Append("/");

                builder.Append(path);

                if (!Uri.TryCreate(builder.ToString(), UriKind.Absolute, out uri))
                    return null; // UNABLE TO READ URI FROM CLIENT
            }

            var plainAuthority = new Authority(uri.Host, _reverseModeForcedPort ?? uri.Port, false);
            var plainExchangeContext = await contextBuilder.Create(plainAuthority, false).ConfigureAwait(false);

            var bodyStream = SetChunkedBody(plainHeader, plainStream);

            return new ExchangeSourceInitResult(
                plainAuthority,
                plainStream,
                plainStream,
                new Exchange(_idProvider,
                    plainExchangeContext,
                    plainAuthority,
                    plainHeader, bodyStream, "HTTP/1.1", receivedFromProxy), false);
        }
    }
}
