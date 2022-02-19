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
        private readonly CancellationToken _poolCancellationToken;
        private readonly UpStreamChannel _upStreamChannel;
        private readonly IHeaderEncoder _headerEncoder;
        
        private readonly Memory<byte> _dataReceptionBuffer;
        private readonly H2StreamSetting _globalSetting;
        private readonly Http11Parser _parser;
        private readonly H2Logger _logger;
        private readonly HPackDecoder _hPackDecoder;
        
        private readonly CancellationTokenSource _currentStreamCancellationTokenSource;
        private readonly MutableMemoryOwner<byte> _receptionBufferContainer;

        private CancellationToken _overallToken;


        private H2ErrorCode _resetErrorCode;
        private bool _noBodyStream = false;

        private int _currentdReceived = 0;

        private bool _disposed = false;
        
        private readonly Pipe _pipeResponseBody;

        private int _totalBodyReceived;

        private bool _firstBodyFragment = true;

        public StreamManager(
            int streamIdentifier,
            StreamPool parent,
            Exchange exchange, 
            UpStreamChannel upStreamChannel,
            IHeaderEncoder headerEncoder,
            H2StreamSetting globalSetting,
            WindowSizeHolder overallWindowSizeHolder,
            Http11Parser parser, 
            H2Logger logger,
            HPackDecoder hPackDecoder,
            CancellationToken mainLoopCancellationToken,
            CancellationToken poolCancellationToken)
        {
            StreamIdentifier = streamIdentifier;
            _parent = parent;
            _exchange = exchange;
            _poolCancellationToken = poolCancellationToken;
            _upStreamChannel = upStreamChannel;
            _headerEncoder = headerEncoder;
            
            RemoteWindowSize = new WindowSizeHolder(logger,
                globalSetting.OverallWindowSize, 
                streamIdentifier);

            OverallRemoteWindowSize = overallWindowSizeHolder; 
            
            _globalSetting = globalSetting;
            _parser = parser;
            _logger = logger;
            _hPackDecoder = hPackDecoder;

            _receptionBufferContainer = MemoryPool<byte>.Shared.RendExact(16 * 1024);

            _dataReceptionBuffer = _receptionBufferContainer.Memory;
            
            _currentStreamCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(poolCancellationToken, mainLoopCancellationToken);

            _currentStreamCancellationTokenSource.Token.Register(OnStreamHaltRequest);

            _overallToken = _currentStreamCancellationTokenSource.Token;

            _pipeResponseBody = new Pipe(new PipeOptions());
            
        }

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

            if (_poolCancellationToken.IsCancellationRequested)
            {
                // The caller cancelled the request 

                _pipeResponseBody.Writer.Complete();
                //_pipeResponseBody.Reader.Complete();

                return;
            }

            // In case of exception in the main loop, we ensure that wait tasks for the caller are 
            // properly interrupt with exception

            var goAwayException = _parent.GoAwayException;

            _pipeResponseBody.Writer.Complete();
            //_pipeResponseBody.Reader.Complete();
        }
        
        public int StreamIdentifier { get;  }

        public byte StreamPriority { get; private set; }

        public int StreamDependency { get; private set; }

        public bool Exclusive { get; private set; }
        public WindowSizeHolder RemoteWindowSize { get;  }
        
        public WindowSizeHolder OverallRemoteWindowSize { get;  }

        public StreamPool Parent => _parent;

        private async ValueTask<int> BookWindowSize(int requestedBodyLength, CancellationToken cancellationToken)
        {
            if (requestedBodyLength == 0)
                return 0;

            var wa = RemoteWindowSize.WindowSize;
            var streamWindow = await RemoteWindowSize.BookWindowSize(requestedBodyLength, cancellationToken).ConfigureAwait(false);
            var wz = RemoteWindowSize.WindowSize; 

            if (streamWindow == 0)
                return 0;

            var overallWindow = await OverallRemoteWindowSize.BookWindowSize(streamWindow, cancellationToken)
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
            _currentStreamCancellationTokenSource.Cancel(true);
            
            _pipeResponseBody.Writer.Complete();
                
           // _responseBodyComplete.TrySetResult(null);

            if (errorCode == H2ErrorCode.EnhanceYourCalm)
            {

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
        
        public Task EnqueueRequestHeader(Exchange exchange)
        {
            var endStream = exchange.Request.Header.ContentLength == 0 ||
                            exchange.Request.Body == null ||
                            exchange.Request.Body.CanSeek && exchange.Request.Body.Length == 0;

            var readyToBeSent = _headerEncoder.Encode(
                new HeaderEncodingJob(exchange.Request.Header.RawHeader, StreamIdentifier, StreamDependency), 
                _dataReceptionBuffer, endStream);

            exchange.Metrics.RequestHeaderSending = ITimingProvider.Default.Instant();

            var writeHeaderTask = new WriteTask(H2FrameType.Headers, StreamIdentifier, StreamPriority,
                StreamDependency, readyToBeSent);

            _upStreamChannel(ref writeHeaderTask);

            return writeHeaderTask.DoneTask
                .ContinueWith(t => _exchange.Metrics.TotalSent += readyToBeSent.Length, _poolCancellationToken);
        }

        public async Task ProcessRequestBody(Exchange exchange)
        {
            var totalSent = 0;
            Stream requestBodyStream = exchange.Request.Body;
            long bodyLength = exchange.Request.Header.ContentLength;

            if (requestBodyStream != null 
                && (!requestBodyStream.CanSeek || requestBodyStream.Length > 0))
            {

                while (true)
                {
                    var bookedSize = _globalSetting.Remote.MaxFrameSize - 9;

                    if (_disposed)
                        throw new TaskCanceledException("Stream cancellation request");

                    // We check wait for available Window Size from remote
                    
                    bookedSize = await BookWindowSize(bookedSize, _overallToken)
                        .ConfigureAwait(false);
                    
                    if (bookedSize == 0)
                        throw new TaskCanceledException("Stream cancellation request");


                    var dataFramePayloadLength = await requestBodyStream
                        .ReadAsync(_dataReceptionBuffer.Slice(9, bookedSize),
                            _overallToken)
                        .ConfigureAwait(false);

                    if (dataFramePayloadLength < bookedSize)
                    {
                        // window size refund 
                        RemoteWindowSize.UpdateWindowSize(bookedSize - dataFramePayloadLength);
                        OverallRemoteWindowSize.UpdateWindowSize(bookedSize - dataFramePayloadLength);
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

                    _upStreamChannel(ref writeTaskBody);

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

        private Memory<byte> _headerBuffer;
        private int _totalHeaderReceived;
        private readonly SemaphoreSlim _headerReceivedSemaphore = new SemaphoreSlim(0, 1);
        private bool _complete;

        private void ReceiveHeaderFragmentFromConnection(ReadOnlyMemory<byte> buffer,
            bool lastHeaderFragment)
        {
            if (_headerBuffer.IsEmpty)
            {
                _headerBuffer = new byte[_globalSetting.MaxHeaderSize]; 
            }

            buffer.CopyTo(_headerBuffer.Slice(_totalHeaderReceived));
            _totalHeaderReceived += buffer.Length; 

            if (lastHeaderFragment)
            {
                _exchange.Metrics.ResponseHeaderEnd = ITimingProvider.Default.Instant();

                var charHeader = DecodeAndAllocate(_headerBuffer.Slice(0, _totalHeaderReceived).Span);

                _exchange.Response.Header =
                    new ResponseHeader(charHeader, true, _parser);

                _logger.TraceResponse(this, _exchange);

                _logger.Trace(StreamIdentifier, "Releasing semaphore");

                _headerReceivedSemaphore.Release();
            }
        }

        private Memory<char> DecodeAndAllocate(ReadOnlySpan<byte> onWire)
        {
            Span<char> tempBuffer = stackalloc char[_globalSetting.MaxHeaderSize];

            var decoded = _hPackDecoder.Decode(onWire, tempBuffer);
            Memory<char> charBuffer = new char[decoded.Length];

            decoded.CopyTo(charBuffer.Span);

            return charBuffer.Slice(0, decoded.Length);
        }

        public async Task ProcessResponse(CancellationToken cancellationToken)
        {
            try
            {
                if (!_overallToken.IsCancellationRequested)
                    await _headerReceivedSemaphore.WaitAsync(_overallToken);

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

        public async Task ReceiveBodyFragmentFromConnection(ReadOnlyMemory<byte> buffer, bool endStream)
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

            var flushResult = await _pipeResponseBody.Writer.WriteAsync(buffer, _poolCancellationToken);

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

            if (totalSendOnStream > 10)
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

            _upStreamChannel(ref writeTask);
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
            OverallRemoteWindowSize?.Dispose();
            
            _currentStreamCancellationTokenSource.Cancel();

            _currentStreamCancellationTokenSource.Dispose();
            _receptionBufferContainer?.Dispose();
            _headerReceivedSemaphore.Dispose();

            _logger.Trace(StreamIdentifier, ".... disposed");

        }
    }
}