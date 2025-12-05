// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Core;
using Fluxzy.Misc.ResizableBuffers;

namespace Fluxzy.Clients.H2
{
    internal sealed class StreamWorker : IDisposable
    {
        private readonly Exchange _exchange;

        private readonly SemaphoreSlim _headerReceivedSemaphore = new(0, 1);

        private readonly H2Logger _logger;

        private readonly Pipe _pipeResponseBody;
        private readonly CancellationTokenSource _resetTokenSource;

        private bool _disposed;
        private bool _firstBodyFragment = true;

        private Memory<byte> _headerBuffer;

        private bool _noBodyStream;

        private int _totalBodyReceived;

        private int _totalHeaderReceived;

        private int _totalSendOnStream;

        public StreamWorker(
            int streamIdentifier,
            StreamPool parent,
            Exchange exchange, CancellationTokenSource resetTokenSource)
        {
            StreamIdentifier = streamIdentifier;
            Parent = parent;
            _exchange = exchange;
            _resetTokenSource = resetTokenSource;

            RemoteWindowSize = new WindowSizeHolder(parent.Context.Logger,
                parent.Context.Setting.OverallWindowSize,
                streamIdentifier);

            _logger = parent.Context.Logger;

            _pipeResponseBody = new Pipe(new PipeOptions(MemoryPool<byte>.Shared));
        }

        public int StreamIdentifier { get; }

        public byte StreamPriority { get; private set; }

        public int StreamDependency { get; private set; }

        public bool Exclusive { get; private set; }

        public WindowSizeHolder RemoteWindowSize { get; }

        public StreamPool Parent { get; }

        public void Dispose()
        {
            _disposed = true;

            _logger.Trace(StreamIdentifier, ".... disposing");

            RemoteWindowSize?.Dispose();

            try {
                _headerReceivedSemaphore.Release();
                _headerReceivedSemaphore.Dispose();
            }
            catch (SemaphoreFullException) {
                // We do nothing here
            }

            _logger.Trace(StreamIdentifier, ".... disposed");
        }

        private async ValueTask<int> BookWindowSize(int requestedBodyLength, CancellationToken cancellationToken)
        {
            if (requestedBodyLength == 0)
                return 0;

            var streamWindow = await RemoteWindowSize
                                            .BookWindowSize(requestedBodyLength, cancellationToken)
                                            .ConfigureAwait(false);

            if (streamWindow == 0)
                return 0;

            var overallWindow = await Parent.Context.OverallWindowSizeHolder
                                            .BookWindowSize(streamWindow, cancellationToken)
                                            .ConfigureAwait(false);

            return overallWindow;
        }

        public void NotifyStreamWindowUpdate(int windowSizeUpdateValue)
        {
            RemoteWindowSize.UpdateWindowSize(windowSizeUpdateValue);
        }

        public void ResetByCaller(H2ErrorCode reason = H2ErrorCode.StreamClosed)
        {
            var buffer = new byte[13];

            var frame = new RstStreamFrame(StreamIdentifier, reason);

            frame.Write(buffer);

            var writeTask = new WriteTask(
                H2FrameType.RstStream,
                StreamIdentifier,
                StreamPriority,
                StreamDependency,
                buffer);

            Parent.Context.UpStreamChannel(ref writeTask);
        }

        public void ResetRequest(H2ErrorCode errorCode)
        {
            if (!_resetTokenSource.IsCancellationRequested)
                _resetTokenSource.Cancel();

            _pipeResponseBody.Writer.Complete();

            if (errorCode != H2ErrorCode.NoError) {
                var value = _exchange.Request.Header.GetHttp11Header().ToString();

                if (_exchange.Response.Header != null)
                    value += _exchange.Response.Header.GetHttp11Header().ToString();

                _logger.Trace(StreamIdentifier, $"Receive RST : {errorCode} from server.\r\n{value}");
            }

            _exchange.ExchangeCompletionSource
                     .TrySetException(new ExchangeException($"Receive RST : {errorCode} from server"));

            Parent.NotifyDispose(this);
        }

        public void SetPriority(ref PriorityFrame priorityFrame)
        {
            StreamDependency = priorityFrame.StreamDependency;
            StreamPriority = priorityFrame.Weight;
            Exclusive = priorityFrame.Exclusive;
        }

        public Task EnqueueRequestHeader(Exchange exchange, RsBuffer buffer, CancellationToken token)
        {
            var endStream = exchange.Request.Header.ContentLength == 0 ||
                            exchange.Request.Body == null ||
                            (exchange.Request.Body.CanSeek && exchange.Request.Body.Length == 0);

            var readyToBeSent = Parent.Context.HeaderEncoder.Encode(
                new HeaderEncodingJob(exchange.Request.Header.GetHttp11Header(), StreamIdentifier, StreamDependency),
                buffer, endStream);

            exchange.Metrics.RequestHeaderSending = ITimingProvider.Default.Instant();

            var writeHeaderTask = new WriteTask(H2FrameType.Headers, StreamIdentifier, StreamPriority,
                StreamDependency, readyToBeSent);

            Parent.Context.UpStreamChannel(ref writeHeaderTask);

            return writeHeaderTask.DoneTask
                                  .ContinueWith(t => {
                                      exchange.Metrics.RequestHeaderLength = readyToBeSent.Length;

                                      return _exchange.Metrics.TotalSent += readyToBeSent.Length;
                                  }, token);
        }

        public async ValueTask ProcessRequestBody(Exchange exchange, RsBuffer buffer, CancellationToken token)
        {
            var totalSent = 0;
            var requestBodyStream = exchange.Request.Body;
            var bodyLength = exchange.Request.Header.ContentLength;
            var localBuffer = buffer.Memory;

            if (requestBodyStream != null
                && (!requestBodyStream.CanSeek || requestBodyStream.Length > 0)) {
                while (true) {
                    var bookedSize = Math.Min(Parent.Context.Setting.Local.MaxFrameSize, buffer.Buffer.Length) - 9;

                    if (_disposed)
                        throw new TaskCanceledException("Stream cancellation request");

                    // We check wait for available Window Size from remote
                    bookedSize = await BookWindowSize(bookedSize, token)
                        .ConfigureAwait(false);

                    if (bookedSize == 0)
                        throw new TaskCanceledException("Stream cancellation request");

                    var dataFramePayloadLength = await requestBodyStream
                                                       .ReadAsync(buffer.Memory.Slice(9, bookedSize), token)
                                                       .ConfigureAwait(false);

                    var refund = bookedSize - dataFramePayloadLength;

                    if (bookedSize > dataFramePayloadLength) {
                        // window size refund 

                        RemoteWindowSize.UpdateWindowSize(refund);
                        Parent.Context.OverallWindowSizeHolder.UpdateWindowSize(refund);
                    }

                    var endStream = dataFramePayloadLength == 0;

                    if (bodyLength >= 0 && totalSent + dataFramePayloadLength >= bodyLength)
                        endStream = true;

                    new DataFrame(endStream ? HeaderFlags.EndStream : HeaderFlags.None, dataFramePayloadLength,
                        StreamIdentifier).WriteHeaderOnly(buffer.Memory.Span, dataFramePayloadLength);

                    var writeTaskBody = new WriteTask(H2FrameType.Data, StreamIdentifier,
                        StreamPriority, StreamDependency,
                        buffer.Memory.Slice(0, 9 + dataFramePayloadLength));

                    Parent.Context.UpStreamChannel(ref writeTaskBody);

                    totalSent += dataFramePayloadLength;

                    _exchange.Metrics.TotalSent += dataFramePayloadLength;

                    if (dataFramePayloadLength == 0 || endStream)
                        return;

                    // This is for back pressure 
                    await writeTaskBody.DoneTask.ConfigureAwait(false);
                }
            }
        }

        internal void ReceiveHeaderFragmentFromConnection(ref HeadersFrame headerFrame)
        {
            _exchange.Metrics.TotalReceived += headerFrame.BodyLength;
            _exchange.Metrics.ResponseHeaderLength += headerFrame.BodyLength;

            if (_exchange.Metrics.ResponseHeaderStart == default)
                _exchange.Metrics.ResponseHeaderStart = ITimingProvider.Default.Instant();

            if (headerFrame.EndStream)
                _noBodyStream = true;

            ReceiveHeaderFragmentFromConnection(headerFrame.Data, headerFrame.EndHeaders);
        }

        internal void ReceiveHeaderFragmentFromConnection(ref ContinuationFrame continuationFrame)
        {
            _exchange.Metrics.TotalReceived += continuationFrame.BodyLength;
            _exchange.Metrics.ResponseHeaderLength += continuationFrame.BodyLength;
            ReceiveHeaderFragmentFromConnection(continuationFrame.Data, continuationFrame.EndHeaders);
        }

        private void ReceiveHeaderFragmentFromConnection(
            ReadOnlyMemory<byte> buffer,
            bool lastHeaderFragment)
        {
            if (_headerBuffer.IsEmpty)
                _headerBuffer = new byte[Parent.Context.Setting.MaxHeaderSize];

            buffer.CopyTo(_headerBuffer.Slice(_totalHeaderReceived));
            _totalHeaderReceived += buffer.Length;

            if (lastHeaderFragment) {
                _exchange.Metrics.ResponseHeaderEnd = ITimingProvider.Default.Instant();

                var charHeader = H2Helper.DecodeAndAllocate(Parent.Context.HeaderEncoder, _headerBuffer.Slice(0, _totalHeaderReceived).Span);

                _exchange.Response.Header = new ResponseHeader(charHeader, true, false);

                if (_exchange.Response.Header.StatusCode == 103)
                {
                    _totalHeaderReceived -= buffer.Length;
                    return; // We wait for more header and ignore 103
                }

                _logger.TraceResponse(this, _exchange);

                if (DebugContext.InsertFluxzyMetricsOnResponseHeader) {
                    var headerName = "fluxzy-h2-debug";

                    var headerValue = $"connectionId = {Parent.Context.ConnectionId} " +
                                      $"- streamId = {StreamIdentifier} - ";

                    _exchange.Response.Header.AddExtraHeaderFieldToLocalConnection(
                        new HeaderField(headerName, headerValue));
                }

                _logger.Trace(StreamIdentifier, "Releasing semaphore");

                _headerReceivedSemaphore.Release();
            }
        }
        
        public async ValueTask ProcessResponse(CancellationToken cancellationToken, H2ConnectionPool cp)
        {
            SendWindowUpdate(Parent.Context.Setting.Local.WindowSize, StreamIdentifier);

            try {
                _logger.Trace(StreamIdentifier, "Before semaphore ");

                if (!cancellationToken.IsCancellationRequested)
                    await _headerReceivedSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                _logger.Trace(StreamIdentifier, "Acquire semaphore ");
            }
            catch (OperationCanceledException) {
                _logger.Trace(StreamIdentifier, $"Received no header, cancelled by caller {StreamIdentifier}");

                throw new ClientErrorException(1,
                    "The connection was interrupted before receiving response header");
            }
            catch (Exception) {
                Parent.NotifyDispose(this);

                throw;
            }

            _exchange.Response.Body = _pipeResponseBody.Reader.AsStream();

            if (_noBodyStream) {
                // This stream as no more body 
                await _pipeResponseBody.Writer.CompleteAsync().ConfigureAwait(false);

                _exchange.ExchangeCompletionSource.TrySetResult(false);
                Parent.NotifyDispose(this);
            }
        }

        public void ReceiveBodyFragmentFromConnection(ReadOnlyMemory<byte> buffer, bool endStream)
        {
            if (_noBodyStream) {
                throw new InvalidOperationException("Receiving response body was not expected. " +
                                                    "Protocol error.");
            }

            _totalBodyReceived += buffer.Length;

            _logger.TraceDeep(StreamIdentifier, () => "a - 1");

            if (_firstBodyFragment) {
                _exchange.Metrics.ResponseBodyStart = ITimingProvider.Default.Instant();
                _firstBodyFragment = false;

                _logger.Trace(_exchange, StreamIdentifier,
                    () => "First body block received");
            }

            _logger.TraceDeep(StreamIdentifier, () => "a - 2");
            OnDataConsumedByCaller(buffer.Length);

            if (endStream) {
                _logger.Trace(_exchange, StreamIdentifier,
                    () => "Total body received : " + _totalBodyReceived);

                _exchange.Metrics.ResponseBodyEnd = ITimingProvider.Default.Instant();
            }

            _exchange.Metrics.TotalReceived += buffer.Length;

            _logger.TraceDeep(StreamIdentifier, () => "a - 3");

            var cancelled = false;

            try {
                _pipeResponseBody.Writer.Write(buffer.Span);
            }
            catch {
                cancelled = true;
            }

            // var flushResult = await _pipeResponseBody.Writer.WriteAsync(buffer, token);

            _logger.TraceDeep(StreamIdentifier, () => "a - 4");

            var shouldEnd = endStream || cancelled;

            if (shouldEnd) {
                if (_exchange.Metrics.ResponseBodyEnd == default)
                    _exchange.Metrics.ResponseBodyEnd = ITimingProvider.Default.Instant();

                _logger.Trace(_exchange, StreamIdentifier,
                    () => "End");

                if (!cancelled)
                    _pipeResponseBody.Writer.Complete();

                _logger.TraceDeep(StreamIdentifier, () => "a - 5");

                _exchange.ExchangeCompletionSource.TrySetResult(false);

                // Give a chance for semaphores to released before disposed

                // await Task.Yield();

                _logger.TraceDeep(StreamIdentifier, () => "a - 6");

                Parent.NotifyDispose(this);
            }
        }

        internal void OnDataConsumedByCaller(int dataSize)
        {
            var windowUpdateValue = Parent.ShouldWindowUpdate(dataSize);

            if (windowUpdateValue > 0)

                //SendWindowUpdate(windowUpdateValue, StreamIdentifier);
                SendWindowUpdate(windowUpdateValue, 0);

            _totalSendOnStream += dataSize;

            if (_totalSendOnStream > 1024 * 16) {
                SendWindowUpdate(_totalSendOnStream, StreamIdentifier);
                _totalSendOnStream = 0;
            }
        }

        private void SendWindowUpdate(int windowSizeUpdateValue, int streamIdentifier)
        {
            var writeTask = new WriteTask(
                H2FrameType.WindowUpdate,
                streamIdentifier, StreamPriority,
                StreamDependency, Memory<byte>.Empty, windowSizeUpdateValue);

            Parent.Context.UpStreamChannel(ref writeTask);
        }

        public override string ToString()
        {
            return $"Stream Identifier : {StreamIdentifier}";
        }
    }
}
