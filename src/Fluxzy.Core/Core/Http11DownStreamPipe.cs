// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.H11;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Streams;
using Fluxzy.Utils;

namespace Fluxzy.Core
{
    internal class Http11DownStreamPipe : IDownStreamPipe
    {
        private readonly IIdProvider _idProvider;
        private readonly IExchangeContextBuilder _contextBuilder;
        private readonly bool _plainAuthorityPerRequest;
        private readonly int? _plainAuthorityForcedPort;
        private static int _count;

        private Stream? _readStream;
        private Stream? _writeStream;

        public Http11DownStreamPipe(
            IIdProvider idProvider,
            Authority requestedAuthority, Stream readStream, Stream writeStream,
            IExchangeContextBuilder contextBuilder,
            bool plainAuthorityPerRequest = false,
            int? plainAuthorityForcedPort = null)
        {
            _idProvider = idProvider;
            _contextBuilder = contextBuilder;
            _plainAuthorityPerRequest = plainAuthorityPerRequest;
            _plainAuthorityForcedPort = plainAuthorityForcedPort;
            RequestedAuthority = requestedAuthority;
            _readStream = readStream;
            _writeStream = writeStream;

            var id = Interlocked.Increment(ref _count);

            if (DebugContext.EnableNetworkFileDump)
            {
                _readStream = new DebugFileStream($"raw/{id:0000}_browser_",
                    _readStream, true);

                _writeStream = new DebugFileStream($"raw/{id:0000}_browser_",
                    _writeStream, false);
            }
        }

        public Authority RequestedAuthority { get; }

        private Stream? ReadStream { get; set; }

        private Stream? WriteStream { get; set; }

        public bool TunnelOnly { get; }

        public virtual async ValueTask<Exchange?> ReadNextExchange(RsBuffer buffer, ExchangeScope exchangeScope, CancellationToken token)
        { 
            if (_readStream == null)
                throw new FluxzyException("Down stream has already been abandoned");

            // Every next request after the first one is read from the stream

            var blockReadResult = await
                Http11HeaderBlockReader.GetNext(_readStream, buffer, null, null, throwOnError: false, token)
                                       .ConfigureAwait(false);

            if (blockReadResult.TotalReadLength == 0)
                return null;

            var receivedFromProxy = ITimingProvider.Default.Instant();

            //var secureHeaderChars = new char[blockReadResult.HeaderLength];

            var secureHeaderChars = exchangeScope.RegisterForReturn(blockReadResult.HeaderLength);

            Encoding.ASCII.GetChars(buffer.Buffer.AsSpan(0, blockReadResult.HeaderLength), secureHeaderChars.Span);

            var secureHeader = new RequestHeader(secureHeaderChars, true);

            var remainingLength = blockReadResult.TotalReadLength - blockReadResult.HeaderLength;

            if (remainingLength > 0)
            {
                // Push leftover bytes back into a single reusable buffer.
                // Nesting a new stream wrapper per request grows without bound
                // on keep-alive connections and ends in a stack overflow (#744)
                if (_readStream is not PushbackReadStream pushbackStream)
                {
                    pushbackStream = new PushbackReadStream(_readStream);
                    _readStream = pushbackStream;
                }

                pushbackStream.Push(buffer.Buffer.AsSpan(blockReadResult.HeaderLength, remainingLength));
            }

            var authority = RequestedAuthority;

            // Plain proxy connections carry a per-request authority: routing with the
            // connection-level one sends the request to whichever host came first
            // (cross-host contamination, 421). Tunnels (CONNECT, SOCKS5) keep it.
            if (_plainAuthorityPerRequest
                && !AuthorityUtility.TryParsePlainRequestAuthority(
                    secureHeader, _plainAuthorityForcedPort, out authority))
                return null;

            var exchangeContext = await _contextBuilder.Create(authority, authority.Secure).ConfigureAwait(false);

            var bodyStream = SetChunkedBody(secureHeader, _readStream);

            return new Exchange(_idProvider, exchangeContext, authority, secureHeader, bodyStream, null!, receivedFromProxy);
        }

        public async ValueTask WriteInterimResponse(int statusCode, ReadOnlyMemory<char> reasonPhrase, int _, CancellationToken token)
        {
            if (_writeStream == null)
                throw new FluxzyException("Down stream has already been closed");

            // "HTTP/1.1 NNN <reason>\r\n\r\n" — max ~64 bytes for typical reason phrases.
            var line = $"HTTP/1.1 {statusCode} {reasonPhrase}\r\n\r\n";
            var bytes = Encoding.ASCII.GetBytes(line);

            await _writeStream.WriteAsync(bytes, 0, bytes.Length, token).ConfigureAwait(false);
            await _writeStream.FlushAsync(token).ConfigureAwait(false);
        }

        public async ValueTask WriteResponseHeader(ResponseHeader responseHeader, RsBuffer buffer, bool shouldClose, int _, ReadOnlyMemory<char> requestMethod, CancellationToken token)
        {
            if (_writeStream == null)
                throw new FluxzyException("Down stream has already been closed");

            if (_writeStream != null)
            {
                var responseHeaderLength = responseHeader.WriteHttp11(false, buffer, true, true, shouldClose);
                await _writeStream.WriteAsync(buffer.Buffer, 0, responseHeaderLength, token).ConfigureAwait(false);
            }
        }

        public async ValueTask WriteResponseBody(Stream responseBodyStream, RsBuffer rsBuffer, bool chunked, int _, Response? responseForTrailers, CancellationToken token)
        {
            if (_writeStream == null)
                throw new FluxzyException("Down stream has already been closed");

            var stream = _writeStream;
            ChunkedTransferWriteStream? chunkedWriter = null;

            if (chunked) {
                chunkedWriter = new ChunkedTransferWriteStream(stream);
                stream = chunkedWriter;
            }

            if (chunked) {
                await responseBodyStream
                    .CopyDetailed(stream, rsBuffer.Buffer, _ => { }, flushAfterEachWrite: true, token)
                    .ConfigureAwait(false);
            }
            else {
                await responseBodyStream
                    .CopyDetailed(
                        stream,
                        FluxzySharedSetting.ResponseBodyCopyBuffer,
                        _ => { },
                        flushAfterEachWrite: false,
                        token)
                    .ConfigureAwait(false);
            }

            if (chunkedWriter != null) {
                // After body is drained, trailers are available on the Response object
                await chunkedWriter.WriteEof(responseForTrailers?.Trailers).ConfigureAwait(false);
            }

            await stream.FlushAsync(CancellationToken.None).ConfigureAwait(false);
        }

        public (Stream ReadStream, Stream WriteStream) AbandonPipe()
        {
            if (_readStream == null || _writeStream == null)
                throw new FluxzyException("Down stream has already been closed");

            var readStream = _readStream;
            var writeStream = _writeStream;

            _readStream = null;
            _writeStream = null;

            return (readStream, writeStream);
        }

        public bool CanWrite => _writeStream != null;

        public bool SupportsMultiplexing => false;

        public static Stream SetChunkedBody(RequestHeader plainHeader, Stream plainStream)
        {
            Stream bodyStream;

            if (plainHeader.ChunkedBody)
                bodyStream = new ChunkedTransferReadStream(plainStream, false);
            else
            {
                bodyStream = plainHeader.ContentLength > 0
                    ? new ContentBoundStream(plainStream, plainHeader.ContentLength)
                    : Stream.Null;
            }

            return bodyStream;
        }

        public void Dispose()
        {
            _readStream?.Dispose();
            _writeStream?.Dispose();
        }
    }
}
