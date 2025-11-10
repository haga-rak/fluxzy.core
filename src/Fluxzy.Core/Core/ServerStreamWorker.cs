// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Misc.ResizableBuffers;

namespace Fluxzy.Core
{
    internal class ServerStreamWorker : IDisposable
    {
        private readonly int _streamIdentifier;
        private readonly IHeaderEncoder _headerEncoder;
        private readonly byte[] _headerBuffer;
        private int _receivedHeaderLength;
        private bool _endHeader;
        private bool _endStream;
        private bool _exchangeCreated;

        private Pipe? _requestBodyPipe;

        private readonly WindowSizeHolder _streamWindowSizeHolder;
        private readonly WindowSizeHolder _overallWindowSizeHolder;
        private readonly H2StreamSetting _h2StreamSetting;

        private bool _disposed;
        private int _unNotifiedWindowSize;

        public ServerStreamWorker(
            int streamIdentifier,
            IHeaderEncoder headerEncoder,
            WindowSizeHolder overallWindowSizeHolder,
            H2StreamSetting h2StreamSetting,
            H2Logger logger)
        {
            _streamIdentifier = streamIdentifier;
            _headerEncoder = headerEncoder;
            _overallWindowSizeHolder = overallWindowSizeHolder;
            _h2StreamSetting = h2StreamSetting;
            _headerBuffer = new byte[h2StreamSetting.MaxHeaderSize];
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
            }

            return H2ErrorCode.NoError;
        }

        /// <summary>
        /// returns false if stream shall be go awayed
        /// </summary>
        /// <param name="headerFrame"></param>
        /// <returns></returns>
        public H2ErrorCode ProcessHeaderFrame(ref H2FrameReadResult frame)
        {
            var headerFrame = frame.GetHeadersFrame();

            if (headerFrame.EndStream) {
                _endStream = true;
            }

            return ReceiveHeaderFragment(headerFrame.Data.Span, headerFrame.EndHeaders);
        }

        public H2ErrorCode ProcessContinuation(ref H2FrameReadResult frame)
        {
            var continuationFrame = frame.GetContinuationFrame(); 
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

            await _requestBodyPipe.Writer.WriteAsync(buffer.Memory.Slice(0, length), token);

            if (endStream)
            {
                _endStream = true;
                await _requestBodyPipe.Writer.CompleteAsync();
            }

            _unNotifiedWindowSize += length;

            int? notified = null;

            if (_unNotifiedWindowSize > _h2StreamSetting.Local.WindowSize) {

                notified = _unNotifiedWindowSize;
                _unNotifiedWindowSize = 0;
            }

            return new (H2ErrorCode.NoError, length, notified);
        }

        public bool ReadyToCreateExchange => _endHeader && !_exchangeCreated;

        public async Task<Exchange> CreateExchange(
            IIdProvider idProvider,
            IExchangeContextBuilder contextBuilder, 
            Authority authority, bool secure)

        {
            _exchangeCreated = true; 

            var plainRequest =
                H2Helper.DecodeAndAllocate(_headerEncoder, _headerBuffer.AsSpan(0, _receivedHeaderLength));
            var receivedFromProxy = ITimingProvider.Default.Instant();

            var requestHeader = new RequestHeader(plainRequest, true);

            Stream bodyStream;

            if (_endStream) {
                bodyStream = Stream.Null; // no response body
            }
            else {
                _requestBodyPipe = new Pipe(); // TODO configure pipe settings
                bodyStream = _requestBodyPipe.Reader.AsStream();
            }

            var context = await contextBuilder.Create(authority, secure);

            var exchange = new Exchange(idProvider, context, authority, requestHeader, bodyStream, "h2",
                receivedFromProxy) {
                StreamIdentifier = _streamIdentifier
            };

            return exchange;
        }

        public async ValueTask<int> BookWindowSize(int requestedBodyLength, CancellationToken cancellationToken)
        {
            if (requestedBodyLength == 0)
                return 0;

            var streamWindow = await _streamWindowSizeHolder
                                     .BookWindowSize(requestedBodyLength, cancellationToken)
                                     .ConfigureAwait(false);

            if (streamWindow == 0)
                return 0;

            var overallWindow = await _overallWindowSizeHolder
                                            .BookWindowSize(streamWindow, cancellationToken)
                                            .ConfigureAwait(false);

            return overallWindow;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _requestBodyPipe?.Writer.Complete();
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
