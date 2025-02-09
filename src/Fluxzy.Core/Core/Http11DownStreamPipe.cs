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
    public interface IDownStreamPipe : IDisposable
    {
        Authority RequestedAuthority { get; }

        bool TunnelOnly { get; }

        ValueTask<Exchange?> ReadNextExchange(RsBuffer buffer, ExchangeScope exchangeScope, CancellationToken token);

        ValueTask WriteResponseHeader(ResponseHeader responseHeader, RsBuffer buffer, bool shouldClose, CancellationToken token);

        ValueTask WriteResponseBody(Stream responseBodyStream, RsBuffer rsBuffer, bool chunked, CancellationToken token);

        (Stream ReadStream, Stream WriteStream) AbandonPipe();

        bool CanWrite { get; }
    }

    internal class Http11DownStreamPipe : IDownStreamPipe
    {
        private readonly IIdProvider _idProvider;
        private readonly IExchangeContextBuilder _contextBuilder;
        private static int _count;

        public Http11DownStreamPipe(IIdProvider idProvider, 
            Authority requestedAuthority, Stream readStream, Stream writeStream, bool tunnelOnly,
            IExchangeContextBuilder contextBuilder)
        {
            _idProvider = idProvider;
            _contextBuilder = contextBuilder;
            RequestedAuthority = requestedAuthority;
            ReadStream = readStream;
            WriteStream = writeStream;
            TunnelOnly = tunnelOnly;

            var id = Interlocked.Increment(ref _count);

            if (DebugContext.EnableNetworkFileDump)
            {
                ReadStream = new DebugFileStream($"raw/{id:0000}_browser_",
                    ReadStream, true);

                WriteStream = new DebugFileStream($"raw/{id:0000}_browser_",
                    WriteStream, false);
            }
        }

        public Authority RequestedAuthority { get; }

        private Stream? ReadStream { get; set; }

        private Stream? WriteStream { get; set; }

        public bool TunnelOnly { get; }

        public virtual async ValueTask<Exchange?> ReadNextExchange(RsBuffer buffer, ExchangeScope exchangeScope, CancellationToken token)
        { 
            if (ReadStream == null)
                throw new FluxzyException("Down stream has already been abandoned");

            // Every next request after the first one is read from the stream

            var blockReadResult = await
                Http11HeaderBlockReader.GetNext(ReadStream, buffer, null, null, throwOnError: false, token)
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
                ReadStream = new CombinedReadonlyStream(false,
                    buffer.Buffer.AsSpan(blockReadResult.HeaderLength, remainingLength), ReadStream);
            }

            var exchangeContext = await _contextBuilder.Create(RequestedAuthority, RequestedAuthority.Secure).ConfigureAwait(false);

            var bodyStream = SetChunkedBody(secureHeader, ReadStream);

            return new Exchange(_idProvider, exchangeContext, RequestedAuthority, secureHeader, bodyStream, null!, receivedFromProxy);
        }

        public async ValueTask WriteResponseHeader(ResponseHeader responseHeader, RsBuffer buffer, bool shouldClose, CancellationToken token)
        {
            if (WriteStream == null)
                throw new FluxzyException("Down stream has already been closed");

            if (WriteStream != null)
            {
                var responseHeaderLength = responseHeader.WriteHttp11(false, buffer, true, true, shouldClose);
                await WriteStream.WriteAsync(buffer.Buffer, 0, responseHeaderLength, token).ConfigureAwait(false);
            }
        }

        public async ValueTask WriteResponseBody(Stream responseBodyStream, RsBuffer rsBuffer, bool chunked, CancellationToken token)
        {
            if (WriteStream == null)
                throw new FluxzyException("Down stream has already been closed");

            var stream = WriteStream;

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
            if (ReadStream == null || WriteStream == null)
                throw new FluxzyException("Down stream has already been closed");

            var readStream = ReadStream;
            var writeStream = WriteStream;

            ReadStream = null;
            WriteStream = null;

            return (readStream, writeStream);
        }

        public bool CanWrite => WriteStream != null;

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
            ReadStream?.Dispose();
            WriteStream?.Dispose();
        }
    }
}
