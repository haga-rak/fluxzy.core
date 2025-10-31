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

namespace Fluxzy.Core
{
    internal class Http11DownStreamPipe : IDownStreamPipe
    {
        private readonly IIdProvider _idProvider;
        private readonly IExchangeContextBuilder _contextBuilder;
        private static int _count;

        private Stream? _readStream;
        private Stream? _writeStream;

        public Http11DownStreamPipe(
            IIdProvider idProvider,
            Authority requestedAuthority, Stream readStream, Stream writeStream,
            IExchangeContextBuilder contextBuilder)
        {
            _idProvider = idProvider;
            _contextBuilder = contextBuilder;
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
                _readStream = new CombinedReadonlyStream(false,
                    buffer.Buffer.AsSpan(blockReadResult.HeaderLength, remainingLength), _readStream);
            }

            var exchangeContext = await _contextBuilder.Create(RequestedAuthority, RequestedAuthority.Secure).ConfigureAwait(false);

            var bodyStream = SetChunkedBody(secureHeader, _readStream);

            return new Exchange(_idProvider, exchangeContext, RequestedAuthority, secureHeader, bodyStream, null!, receivedFromProxy);
        }

        public async ValueTask WriteResponseHeader(ResponseHeader responseHeader, RsBuffer buffer, bool shouldClose, int _, CancellationToken token)
        {
            if (_writeStream == null)
                throw new FluxzyException("Down stream has already been closed");

            if (_writeStream != null)
            {
                var responseHeaderLength = responseHeader.WriteHttp11(false, buffer, true, true, shouldClose);
                await _writeStream.WriteAsync(buffer.Buffer, 0, responseHeaderLength, token).ConfigureAwait(false);
            }
        }

        public async ValueTask WriteResponseBody(Stream responseBodyStream, RsBuffer rsBuffer, bool chunked, int _, CancellationToken token)
        {
            if (_writeStream == null)
                throw new FluxzyException("Down stream has already been closed");

            var stream = _writeStream;

            if (chunked) {
                stream =
                    new ChunkedTransferWriteStream(stream);
            }

            await responseBodyStream.CopyDetailed(stream, rsBuffer.Buffer, _ => { }, token).ConfigureAwait(false);
            (stream as ChunkedTransferWriteStream)?.WriteEof();
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
