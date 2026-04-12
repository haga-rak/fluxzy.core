// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Misc.ResizableBuffers;

namespace Fluxzy.Core
{
    internal class ServerStreamWorker : IDisposable
    {
        public int StreamIdentifier { get; }

        private readonly IHeaderEncoder _headerEncoder;
        private readonly byte[] _headerBuffer;
        private int _receivedHeaderLength;
        private bool _endHeader;
        private bool _endStream;
        private bool _exchangeCreated;
        private bool _initialHeadersComplete;
        private int _receivedTrailerLength;
        private Exchange? _createdExchange;

        private Pipe? _requestBodyPipe;

        private readonly WindowSizeHolder _streamWindowSizeHolder;
        private readonly H2StreamSetting _h2StreamSetting;

        private bool _disposed;
        private int _unNotifiedWindowSize;

        /// <summary>
        ///     Pre-booked window credits served to callers without touching holders.
        ///     Only accessed from the single write-loop task — no synchronization needed.
        /// </summary>
        private int _windowBudget;

        /// <summary>
        ///     Number of frames worth of window to book at once from the stream holder.
        ///     Reduces async calls when the stream window is large.
        /// </summary>
        private const int BatchFrames = 4;

        public ServerStreamWorker(
            int streamIdentifier,
            IHeaderEncoder headerEncoder,
            H2StreamSetting h2StreamSetting,
            H2Logger logger)
        {
            StreamIdentifier = streamIdentifier;
            _headerEncoder = headerEncoder;
            _h2StreamSetting = h2StreamSetting;
            _headerBuffer = ArrayPool<byte>.Shared.Rent(h2StreamSetting.MaxHeaderSize);
            _streamWindowSizeHolder = new WindowSizeHolder(logger, h2StreamSetting.Remote.WindowSize, streamIdentifier);
        }

        private H2ErrorCode ReceiveHeaderFragment(ReadOnlySpan<byte> data, bool endHeaders)
        {
            var futureHeaderLength = _receivedHeaderLength + data.Length;

            if (futureHeaderLength > _headerBuffer.Length)
                return H2ErrorCode.FrameSizeError;

            data.CopyTo(_headerBuffer.AsSpan(_receivedHeaderLength));

            _receivedHeaderLength = futureHeaderLength;

            if (endHeaders)
            {
                _endHeader = true;
                _initialHeadersComplete = true;
            }

            return H2ErrorCode.NoError;
        }

        private H2ErrorCode ReceiveTrailerFragment(ReadOnlySpan<byte> data, bool endHeaders)
        {
            var futureLength = _receivedTrailerLength + data.Length;

            if (futureLength > _headerBuffer.Length)
                return H2ErrorCode.FrameSizeError;

            data.CopyTo(_headerBuffer.AsSpan(_receivedTrailerLength));
            _receivedTrailerLength = futureLength;

            if (endHeaders && _createdExchange != null)
            {
                var trailerFields = _headerEncoder.Decoder.DecodeTrailerFields(
                    _headerBuffer.AsSpan(0, _receivedTrailerLength));

                _createdExchange.Request.Trailers = trailerFields;
            }

            return H2ErrorCode.NoError;
        }

        /// <summary>
        /// returns false if stream shall be go awayed
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public H2ErrorCode ProcessHeaderFrame(ref H2FrameReadResult frame)
        {
            var headerFrame = frame.GetHeadersFrame();

            if (headerFrame.EndStream) {
                _endStream = true;
            }

            if (_initialHeadersComplete) {
                // This is a trailing HEADERS frame (after body)
                var result = ReceiveTrailerFragment(headerFrame.Data.Span, headerFrame.EndHeaders);

                if (_endStream && _requestBodyPipe != null) {
                    _requestBodyPipe.Writer.Complete();
                }

                return result;
            }

            return ReceiveHeaderFragment(headerFrame.Data.Span, headerFrame.EndHeaders);
        }

        public H2ErrorCode ProcessContinuation(ref H2FrameReadResult frame)
        {
            var continuationFrame = frame.GetContinuationFrame();

            if (_initialHeadersComplete) {
                return ReceiveTrailerFragment(continuationFrame.Data.Span, continuationFrame.EndHeaders);
            }

            return ReceiveHeaderFragment(continuationFrame.Data.Span, continuationFrame.EndHeaders);
        }

        public async Task<ReceiveBodyResult> ReceiveBodyFragment(H2FrameReadResult frame, RsBuffer buffer, CancellationToken token)
        {
            var length = frame.GetDataFrame().Buffer.Length;
            buffer.Ensure(length);
            frame.GetDataFrame().Buffer.CopyTo(buffer.Memory);
            var endStream = frame.GetDataFrame().EndStream;

            if (_requestBodyPipe == null)
            {
                // unexpected data frame
                return new (H2ErrorCode.ProtocolError, 0, null);
            }

            await _requestBodyPipe.Writer.WriteAsync(buffer.Memory.Slice(0, length), token).ConfigureAwait(false);
            await _requestBodyPipe.Writer.FlushAsync(token).ConfigureAwait(false);

            if (endStream)
            {
                _endStream = true;
                await _requestBodyPipe.Writer.CompleteAsync().ConfigureAwait(false);
            }

            _unNotifiedWindowSize += length;

            int? notified = null;

            if (_unNotifiedWindowSize > (_h2StreamSetting.Local.WindowSize / 2)) {

                notified = _unNotifiedWindowSize;
                _unNotifiedWindowSize = 0;
            }

            return new (H2ErrorCode.NoError, length, notified);
        }

        public bool ReadyToCreateExchange => _endHeader && !_exchangeCreated;

        public async ValueTask<Exchange> CreateExchange(
            IIdProvider idProvider,
            IExchangeContextBuilder contextBuilder,
            Authority authority, bool secure)

        {
            _exchangeCreated = true;

            var plainRequest =
                H2Helper.DecodeAndAllocate(_headerEncoder, _headerBuffer.AsSpan(0, _receivedHeaderLength));

            _receivedHeaderLength = 0; // Reset for possible trailer accumulation

            var receivedFromProxy = ITimingProvider.Default.Instant();

            var requestHeader = new RequestHeader(plainRequest, true);

            Stream bodyStream;

            if (_endStream) {
                bodyStream = Stream.Null; // no response body
            }
            else {
                _requestBodyPipe = new Pipe(new PipeOptions(
                    pool: System.Buffers.MemoryPool<byte>.Shared,
                    pauseWriterThreshold: _h2StreamSetting.Local.WindowSize,
                    resumeWriterThreshold: _h2StreamSetting.Local.WindowSize / 2,
                    minimumSegmentSize: _h2StreamSetting.Local.MaxFrameSize,
                    useSynchronizationContext: false));
                bodyStream = _requestBodyPipe.Reader.AsStream();
            }

            var context = await contextBuilder.Create(authority, secure).ConfigureAwait(false);

            var exchange = new Exchange(idProvider, context, authority, requestHeader, bodyStream, "h2",
                receivedFromProxy) {
                StreamIdentifier = StreamIdentifier
            };

            _createdExchange = exchange;

            return exchange;
        }

        public void UpdateWindowSize(int windowSizeIncrement)
        {
            _streamWindowSizeHolder.UpdateWindowSize(windowSizeIncrement);
        }

        public async ValueTask<int> BookWindowSize(int requestedBodyLength, CancellationToken cancellationToken)
        {
            if (requestedBodyLength == 0)
                return 0;

            // Fast path: serve from pre-booked local budget (zero holder calls).
            if (_windowBudget >= requestedBodyLength)
            {
                _windowBudget -= requestedBodyLength;
                return requestedBodyLength;
            }

            // Slow path: book a batch from the stream holder to replenish the budget.
            var batchRequest = requestedBodyLength * BatchFrames;

            var streamWindow = await _streamWindowSizeHolder
                                     .BookWindowSize(batchRequest, cancellationToken)
                                     .ConfigureAwait(false);

            if (streamWindow == 0)
                return 0;

            // Add newly booked amount to existing budget and serve the request.
            _windowBudget += streamWindow;

            var grant = Math.Min(_windowBudget, requestedBodyLength);
            _windowBudget -= grant;

            return grant;
        }

        public void RefundWindowSize(int amount)
        {
            if (amount <= 0)
                return;

            // Return to local budget — the bytes were already booked from the stream holder.
            _windowBudget += amount;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _requestBodyPipe?.Writer.Complete();
            _streamWindowSizeHolder.Dispose();

            ArrayPool<byte>.Shared.Return(_headerBuffer);
        }
    }

    internal readonly record struct ReceiveBodyResult
    {
        public ReceiveBodyResult(H2ErrorCode h2ErrorCode, int bodyLength, int? windowSizeUpdateLength)
        {
            H2ErrorCode = h2ErrorCode;
            BodyLength = bodyLength;
            WindowSizeUpdateLength = windowSizeUpdateLength;
        }

        public void Deconstruct(out H2ErrorCode h2ErrorCode, out int bodyLength, out int? windowSizeUpdateLength)
        {
            h2ErrorCode = H2ErrorCode;
            bodyLength = BodyLength;
            windowSizeUpdateLength = WindowSizeUpdateLength;
        }

        public H2ErrorCode H2ErrorCode { get; }

        public int BodyLength { get; }

        public int? WindowSizeUpdateLength { get; }
    }
}
