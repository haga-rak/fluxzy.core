using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Frames;

namespace Fluxzy.Clients.H2
{
    internal sealed class StreamWorker: IDisposable
    {
        private readonly StreamPool _parent;
        private readonly Exchange _exchange;
        private readonly CancellationTokenSource _resetTokenSource;

        private readonly H2Logger _logger;

        private bool _noBodyStream;
        
        private bool _disposed;
        
        private readonly Pipe _pipeResponseBody;

        private int _totalBodyReceived;
        private bool _firstBodyFragment = true;


        private Memory<byte> _headerBuffer;

        private int _totalHeaderReceived;

        private readonly SemaphoreSlim _headerReceivedSemaphore = new(0, 1);
        

        public StreamWorker(
            int streamIdentifier,
            StreamPool parent,
            Exchange exchange, CancellationTokenSource resetTokenSource)
        {
            StreamIdentifier = streamIdentifier;
            _parent = parent;
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

        public StreamPool Parent => _parent;

        private async ValueTask<int> BookWindowSize(int requestedBodyLength, CancellationToken cancellationToken)
        {
            if (requestedBodyLength == 0)
                return 0;
            
            var streamWindow = await RemoteWindowSize.BookWindowSize(requestedBodyLength, cancellationToken).ConfigureAwait(false);

            if (streamWindow == 0)
                return 0;

            var overallWindow = await Parent.Context.OverallWindowSizeHolder.BookWindowSize(streamWindow, cancellationToken)
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
            {
                _resetTokenSource.Cancel();
            }

            _pipeResponseBody.Writer.Complete();

            if (errorCode != H2ErrorCode.NoError)
            {
                string value = _exchange.Request.Header.GetHttp11Header().ToString();

                if (_exchange.Response.Header != null)
                {
                    value += _exchange.Response.Header.GetHttp11Header().ToString();
                }

                _logger.Trace(StreamIdentifier, $"Receive RST : {errorCode} from server.\r\n{value}");
            }

            _exchange.ExchangeCompletionSource
                .TrySetException(new ExchangeException($"Receive RST : {errorCode} from server"));

            _parent.NotifyDispose(this);
        }

        public void SetPriority(PriorityFrame priorityFrame)
        {
            StreamDependency = priorityFrame.StreamDependency;
            StreamPriority = priorityFrame.Weight;
            Exclusive = priorityFrame.Exclusive; 
        }
        
        public Task EnqueueRequestHeader(Exchange exchange, byte [] buffer, CancellationToken token)
        {
            var endStream = exchange.Request.Header.ContentLength == 0 ||
                            exchange.Request.Body == null ||
                            exchange.Request.Body.CanSeek && exchange.Request.Body.Length == 0;
            
            var readyToBeSent = _parent.Context.HeaderEncoder.Encode(
                new HeaderEncodingJob(exchange.Request.Header.GetHttp11Header(), StreamIdentifier, StreamDependency),
                buffer, endStream);

            exchange.Metrics.RequestHeaderSending = ITimingProvider.Default.Instant();

            var writeHeaderTask = new WriteTask(H2FrameType.Headers, StreamIdentifier, StreamPriority,
                StreamDependency, readyToBeSent);

            _parent.Context.UpStreamChannel(ref writeHeaderTask);

            return writeHeaderTask.DoneTask
                .ContinueWith(t => _exchange.Metrics.TotalSent += readyToBeSent.Length, token);
        }

        public async ValueTask ProcessRequestBody(Exchange exchange, byte[] buffer, CancellationToken token)
        {
            var totalSent = 0;
            var requestBodyStream = exchange.Request.Body;
            var bodyLength = exchange.Request.Header.ContentLength;
            var localBuffer = new Memory<byte>(buffer);

            if (requestBodyStream != null 
                && (!requestBodyStream.CanSeek || requestBodyStream.Length > 0))
            {

                while (true)
                {
                    var bookedSize = _parent.Context.Setting.Local.MaxFrameSize - 9;

                    if (_disposed)
                        throw new TaskCanceledException("Stream cancellation request");

                    // We check wait for available Window Size from remote
                    
                    bookedSize = await BookWindowSize(bookedSize, token)
                        .ConfigureAwait(false);
                    
                    if (bookedSize == 0)
                        throw new TaskCanceledException("Stream cancellation request");

                    var dataFramePayloadLength = await requestBodyStream
                        .ReadAsync(localBuffer.Slice(9, bookedSize),
                            token)
                        .ConfigureAwait(false);

                    if (dataFramePayloadLength < bookedSize)
                    {
                        // window size refund 
                        RemoteWindowSize.UpdateWindowSize(bookedSize - dataFramePayloadLength);
                        Parent.Context.OverallWindowSizeHolder.UpdateWindowSize(bookedSize - dataFramePayloadLength);
                    }

                    var endStream = dataFramePayloadLength == 0;

                    if (bodyLength >= 0 && (totalSent + dataFramePayloadLength) >= bodyLength)
                    {
                        endStream = true;
                    }

                    new DataFrame(endStream ? HeaderFlags.EndStream : HeaderFlags.None, dataFramePayloadLength,
                        StreamIdentifier).WriteHeaderOnly(localBuffer.Span, dataFramePayloadLength);

                    var writeTaskBody = new WriteTask(H2FrameType.Data, StreamIdentifier,
                        StreamPriority, StreamDependency,
                        localBuffer.Slice(0, 9 + dataFramePayloadLength));

                    _parent.Context.UpStreamChannel(ref writeTaskBody);

                    totalSent += dataFramePayloadLength;
                    _exchange.Metrics.TotalSent += dataFramePayloadLength;


                    if (dataFramePayloadLength > 0)
                        NotifyStreamWindowUpdate(dataFramePayloadLength);

                    if (dataFramePayloadLength == 0 || endStream)
                    {
                        return;
                    }

                    await writeTaskBody.DoneTask.ConfigureAwait(false);
                }
            }
        }

        internal void ReceiveHeaderFragmentFromConnection(
            int bodyLength, bool endStream, bool endHeader, ReadOnlyMemory<byte> data)
        {
            _exchange.Metrics.TotalReceived += bodyLength;

            if (_exchange.Metrics.ResponseHeaderStart == default)
                _exchange.Metrics.ResponseHeaderStart = ITimingProvider.Default.Instant();

            if (endStream)
            {
                _noBodyStream = true; 
            }
            ReceiveHeaderFragmentFromConnection(data, endHeader);
        }
        
        internal void ReceiveHeaderFragmentFromConnection(int bodyLength, bool endHeader, ReadOnlyMemory<byte> data)
        {
            _exchange.Metrics.TotalReceived += bodyLength;
            ReceiveHeaderFragmentFromConnection(data, endHeader);
        }

        private void ReceiveHeaderFragmentFromConnection(ReadOnlyMemory<byte> buffer,
            bool lastHeaderFragment)
        {
            if (_headerBuffer.IsEmpty)
            {
                _headerBuffer = new byte[_parent.Context.Setting.MaxHeaderSize]; 
            }

            buffer.CopyTo(_headerBuffer.Slice(_totalHeaderReceived));
            _totalHeaderReceived += buffer.Length; 

            if (lastHeaderFragment)
            {
                _exchange.Metrics.ResponseHeaderEnd = ITimingProvider.Default.Instant();

                var charHeader = DecodeAndAllocate( _headerBuffer.Slice(0, _totalHeaderReceived).Span);

                _exchange.Response.Header = new ResponseHeader(charHeader, true, _parent.Context.Parser);

                _logger.TraceResponse(this, _exchange);

                if (DebugContext.InsertFluxzyMetricsOnResponseHeader)
                {
                    var headerName = "echoes-h2-debug";
                    var headerValue = $"connectionId = {Parent.Context.ConnectionId} " +
                                      $"- streamId = {StreamIdentifier} - ";

                    _exchange.Response.Header.AddExtraHeaderFieldToLocalConnection(
                        new HeaderField(headerName, headerValue));
                }

                _logger.Trace(StreamIdentifier, "Releasing semaphore");

                _headerReceivedSemaphore.Release();
            }
        }

        private Memory<char> DecodeAndAllocate(ReadOnlySpan<byte> onWire)
        {
            Span<char> tempBuffer = stackalloc char[_parent.Context.Setting.MaxHeaderSize];

            var decoded = _parent.Context.HeaderEncoder.Decoder.Decode(onWire, tempBuffer);
            Memory<char> charBuffer = new char[decoded.Length + 256];

            decoded.CopyTo(charBuffer.Span);
            var length = decoded.Length;

            return charBuffer.Slice(0, length);
        }

        public async ValueTask ProcessResponse(CancellationToken cancellationToken)
        {
            SendWindowUpdate(Parent.Context.Setting.Local.WindowSize, StreamIdentifier);

            try
            {
                _logger.Trace(StreamIdentifier, "Before semaphore ");

                if (!cancellationToken.IsCancellationRequested)
                    await _headerReceivedSemaphore.WaitAsync(cancellationToken);

                _logger.Trace(StreamIdentifier, "Acquire semaphore ");
            }
            catch (OperationCanceledException)
            {
                throw new IOException("Received no header, cancelled by caller");
            }
            catch (Exception)
            {
                _parent.NotifyDispose(this);
                throw; 
            }

            _exchange.Response.Body = _pipeResponseBody.Reader.AsStream();

            if (_noBodyStream)
            {
                // This stream as no more body 
                await _pipeResponseBody.Writer.CompleteAsync();

                _exchange.ExchangeCompletionSource.TrySetResult(false);

                _parent.NotifyDispose(this);
            }
        }

        public async Task ReceiveBodyFragmentFromConnection(ReadOnlyMemory<byte> buffer, bool endStream, CancellationToken token)
        {
            if (_noBodyStream)
                throw new InvalidOperationException("Receiving response body was not expected. " +
                                                    "Protocol error.");

            _totalBodyReceived += buffer.Length;

            _logger.TraceDeep(StreamIdentifier, () => "a - 1");

            if (_firstBodyFragment)
            {
                _exchange.Metrics.ResponseBodyStart = ITimingProvider.Default.Instant();
                _firstBodyFragment = false;

                _logger.Trace(_exchange, StreamIdentifier,
                    () => $"First body block received");
                
            }

            _logger.TraceDeep(StreamIdentifier, () => "a - 2");
            OnDataConsumedByCaller(buffer.Length);

            if (endStream)
            {
                _logger.Trace(_exchange, StreamIdentifier,
                    () => $"Total body received : " + _totalBodyReceived);

                _exchange.Metrics.ResponseBodyEnd = ITimingProvider.Default.Instant();
            }
            
            _exchange.Metrics.TotalReceived += buffer.Length;


            _logger.TraceDeep(StreamIdentifier, () => "a - 3");
            
            var flushResult = await _pipeResponseBody.Writer.WriteAsync(buffer, token);

            _logger.TraceDeep(StreamIdentifier, () => "a - 4");

            var shouldEnd = endStream || flushResult.IsCompleted || flushResult.IsCanceled;

            if (shouldEnd)
            {
                if (_exchange.Metrics.ResponseBodyEnd == default)
                {
                    _exchange.Metrics.ResponseBodyEnd = ITimingProvider.Default.Instant();
                }


                _logger.Trace(_exchange, StreamIdentifier,
                    () => $"End");

                if (!flushResult.IsCanceled)
                    await _pipeResponseBody.Writer.CompleteAsync();

                _logger.TraceDeep(StreamIdentifier, () => "a - 5");

                _exchange.ExchangeCompletionSource.TrySetResult(false);
                
                // Give a chance for semaphores to released before disposed

                await Task.Yield();

                _logger.TraceDeep(StreamIdentifier, () => "a - 6");

                _parent.NotifyDispose(this);
            }
        }

        private int totalSendOnStream = 0 ; 

        internal void OnDataConsumedByCaller(int dataSize)
        {
            var windowUpdateValue = Parent.ShouldWindowUpdate(dataSize);

            if (windowUpdateValue > 0)
            {
                //SendWindowUpdate(windowUpdateValue, StreamIdentifier);
                SendWindowUpdate(windowUpdateValue, 0);
            }

            totalSendOnStream += dataSize;

            if (totalSendOnStream > 1024 * 16)
            {
                SendWindowUpdate(totalSendOnStream, StreamIdentifier);
                totalSendOnStream = 0; 
            }
        }

        private void SendWindowUpdate(int windowSizeUpdateValue, int streamIdentifier)
        {
            var writeTask = new WriteTask(
                H2FrameType.WindowUpdate,
                streamIdentifier, StreamPriority, 
                StreamDependency, Memory<byte>.Empty, windowSizeUpdateValue);

            _parent.Context.UpStreamChannel(ref writeTask);
        }

        public override string ToString()
        {
            return $"Stream Id : {StreamIdentifier}"; 
        }

        public void Dispose()
        {
            _disposed = true;

            _logger.Trace(StreamIdentifier, ".... disposing");

            RemoteWindowSize?.Dispose();

            _headerReceivedSemaphore.Release();
            _headerReceivedSemaphore.Dispose();

            _logger.Trace(StreamIdentifier, ".... disposed");

        }
    }
}