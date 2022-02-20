using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Echoes.H2.Encoder;
using Echoes.H2.Encoder.Utils;
using Echoes.Helpers;
using Echoes.IO;

namespace Echoes.H2
{
    internal sealed class StreamManager: IDisposable
    {
        private readonly StreamPool _parent;
        private readonly Exchange _exchange;
        private readonly CancellationTokenSource _resetTokenSource;

        private readonly Memory<byte> _dataReceptionBuffer;
        private readonly H2Logger _logger;
        
        private readonly MutableMemoryOwner<byte> _receptionBufferContainer;

        private H2ErrorCode _resetErrorCode;
        private bool _noBodyStream = false;

        private int _currentdReceived = 0;

        private bool _disposed = false;
        
        private readonly Pipe _pipeResponseBody;

        private int _totalBodyReceived;
        private bool _firstBodyFragment = true;


        private Memory<byte> _headerBuffer;

        private int _totalHeaderReceived;

        private readonly SemaphoreSlim _headerReceivedSemaphore = new(0, 1);

        private bool _complete;

        public StreamManager(
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

            _receptionBufferContainer = MemoryPool<byte>.Shared.RendExact(16 * 1024);
            _dataReceptionBuffer = _receptionBufferContainer.Memory;

            _pipeResponseBody = new Pipe(new PipeOptions());
        }

        public int StreamIdentifier { get; }

        public byte StreamPriority { get; private set; }

        public int StreamDependency { get; private set; }

        public bool Exclusive { get; private set; }

        public WindowSizeHolder RemoteWindowSize { get; }

        public StreamPool Parent => _parent;

        private void OnStreamHaltRequest()
        {
            if (_resetErrorCode != H2ErrorCode.NoError)
            {
                // The stream was reset by remote peer.
                // Send an exception to the caller 

                var rstStreamException = new H2Exception(
                    $"Stream id {StreamIdentifier} halt with a RST_STREAM : {_resetErrorCode}",
                    _resetErrorCode);
                

                return; 
            }

            // In case of exception in the main loop, we ensure that wait tasks for the caller are 
            // properly interrupt with exception

            var goAwayException = _parent.GoAwayException;

            _pipeResponseBody.Writer.Complete();
            //_pipeResponseBody.Reader.Complete();
        }
        

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

        public void ResetRequest(H2ErrorCode errorCode)
        {
            _resetErrorCode = errorCode;

            if (!_resetTokenSource.IsCancellationRequested)
            {
                _resetTokenSource.Cancel();
            }

            _pipeResponseBody.Writer.Complete();

            if (errorCode != H2ErrorCode.NoError)
            {
                string value = _exchange.Request.Header.RawHeader.ToString();

                if (_exchange.Response.Header != null)
                {
                    value += _exchange.Response.Header.RawHeader.ToString();
                }

                Console.WriteLine($"RST : {errorCode} - {_exchange.Authority} - cId : {Parent.Context.ConnectionId} - sId: {StreamIdentifier}");

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
        
        public Task EnqueueRequestHeader(Exchange exchange, CancellationToken token)
        {
            var endStream = exchange.Request.Header.ContentLength == 0 ||
                            exchange.Request.Body == null ||
                            exchange.Request.Body.CanSeek && exchange.Request.Body.Length == 0;

            var readyToBeSent = _parent.Context.HeaderEncoder.Encode(
                new HeaderEncodingJob(exchange.Request.Header.RawHeader, StreamIdentifier, StreamDependency), 
                _dataReceptionBuffer, endStream);

            exchange.Metrics.RequestHeaderSending = ITimingProvider.Default.Instant();

            var writeHeaderTask = new WriteTask(H2FrameType.Headers, StreamIdentifier, StreamPriority,
                StreamDependency, readyToBeSent);

            _parent.Context.UpStreamChannel(ref writeHeaderTask);

            return writeHeaderTask.DoneTask
                .ContinueWith(t => _exchange.Metrics.TotalSent += readyToBeSent.Length, token);
        }

        public async Task ProcessRequestBody(Exchange exchange, CancellationToken token)
        {
            var totalSent = 0;
            Stream requestBodyStream = exchange.Request.Body;
            long bodyLength = exchange.Request.Header.ContentLength;

            if (requestBodyStream != null 
                && (!requestBodyStream.CanSeek || requestBodyStream.Length > 0))
            {

                while (true)
                {
                    var bookedSize = _parent.Context.Setting.Remote.MaxFrameSize - 9;

                    if (_disposed)
                        throw new TaskCanceledException("Stream cancellation request");

                    // We check wait for available Window Size from remote
                    
                    bookedSize = await BookWindowSize(bookedSize, token)
                        .ConfigureAwait(false);
                    
                    if (bookedSize == 0)
                        throw new TaskCanceledException("Stream cancellation request");

                    var dataFramePayloadLength = await requestBodyStream
                        .ReadAsync(_dataReceptionBuffer.Slice(9, bookedSize),
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
                        StreamIdentifier).WriteHeaderOnly(_dataReceptionBuffer.Span, dataFramePayloadLength);

                    var writeTaskBody = new WriteTask(H2FrameType.Data, StreamIdentifier,
                        StreamPriority, StreamDependency,
                        _dataReceptionBuffer.Slice(0, 9 + dataFramePayloadLength));

                    _parent.Context.UpStreamChannel(ref writeTaskBody);

                    totalSent += dataFramePayloadLength;
                    _exchange.Metrics.TotalSent += dataFramePayloadLength;

                    /// ?? Stream window size seems so to not be handled correctly 
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

                var charHeader = DecodeAndAllocate(_headerBuffer.Slice(0, _totalHeaderReceived).Span);

                _exchange.Response.Header = new ResponseHeader(charHeader, true, _parent.Context.Parser);

                _logger.TraceResponse(this, _exchange);

                _logger.Trace(StreamIdentifier, "Releasing semaphore");

                _headerReceivedSemaphore.Release();
            }
        }

        private Memory<char> DecodeAndAllocate(ReadOnlySpan<byte> onWire)
        {
            Span<char> tempBuffer = stackalloc char[_parent.Context.Setting.MaxHeaderSize];

            var decoded = _parent.Context.HeaderEncoder.Decoder.Decode(onWire, tempBuffer);
            Memory<char> charBuffer = new char[decoded.Length];

            decoded.CopyTo(charBuffer.Span);

            return charBuffer.Slice(0, decoded.Length);
        }

        public async Task ProcessResponse(CancellationToken cancellationToken)
        {
            try
            {
                if (!cancellationToken.IsCancellationRequested)
                    await _headerReceivedSemaphore.WaitAsync(cancellationToken);

                _logger.Trace(StreamIdentifier, "Acquire semaphore ");
            }
            catch (OperationCanceledException)
            {
                throw new IOException("Received no header");
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

            bool wasFirstFragment = false; 

            if (_firstBodyFragment)
            {
                _exchange.Metrics.ResponseBodyStart = ITimingProvider.Default.Instant();
                _firstBodyFragment = false;
                wasFirstFragment = true; 

                _logger.Trace(_exchange, StreamIdentifier,
                    () => $"First body block received");
            }

            OnDataConsumedByCaller(buffer.Length);

            if (endStream)
            {
                _logger.Trace(_exchange, StreamIdentifier,
                    () => $"Total body received : " + _totalBodyReceived);

                _exchange.Metrics.ResponseBodyEnd = ITimingProvider.Default.Instant();
            }
            
            _exchange.Metrics.TotalReceived += buffer.Length;

            var flushResult = await _pipeResponseBody.Writer.WriteAsync(buffer, token);

            var shouldEnd = endStream || flushResult.IsCompleted || flushResult.IsCanceled;

            if (shouldEnd)
            {

                _logger.Trace(_exchange, StreamIdentifier,
                    () => $"End");

                if (!flushResult.IsCanceled)
                    await _pipeResponseBody.Writer.CompleteAsync();

                _exchange.ExchangeCompletionSource.TrySetResult(false);
                

                _complete = true; 

                _parent.NotifyDispose(this);
            }

            if (wasFirstFragment)
            {
                _logger.Trace(_exchange, StreamIdentifier,
                    () => $"First body block received well");

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
            _receptionBufferContainer?.Dispose();
            _headerReceivedSemaphore.Dispose();

            _logger.Trace(StreamIdentifier, ".... disposed");

        }
    }
}