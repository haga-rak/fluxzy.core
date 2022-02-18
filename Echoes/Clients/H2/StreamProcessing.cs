using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Echoes.H2.Encoder.Utils;
using Echoes.Helpers;

namespace Echoes.H2
{
    internal class StreamProcessing: IDisposable
    {
        private readonly StreamPool _parent;
        private readonly Exchange _exchange;
        private readonly CancellationToken _callerCancellationToken;
        private readonly UpStreamChannel _upStreamChannel;
        private readonly IHeaderEncoder _headerEncoder;
        private readonly H2Message _response;
        
        private readonly Memory<byte> _dataReceptionBuffer;
        private readonly H2StreamSetting _globalSetting;
        private readonly Http11Parser _parser;

        private readonly TaskCompletionSource<object> _responseHeaderReady = new TaskCompletionSource<object>();
        private readonly TaskCompletionSource<object> _responseBodyComplete = new TaskCompletionSource<object>();

        private readonly CancellationTokenSource _currentStreamCancellationTokenSource;
        private readonly MutableMemoryOwner<byte> _receptionBufferContainer;


        private H2ErrorCode _resetErrorCode;
        private bool _noBodyStream = false; 

        public StreamProcessing(
            int streamIdentifier,
            StreamPool parent,
            Exchange exchange, 
            UpStreamChannel upStreamChannel,
            IHeaderEncoder headerEncoder,
            H2StreamSetting globalSetting,
            WindowSizeHolder overallWindowSizeHolder,
            Http11Parser parser, 
            CancellationToken mainLoopCancellationToken,
            CancellationToken callerCancellationToken)
        {
            StreamIdentifier = streamIdentifier;
            _parent = parent;
            _exchange = exchange;
            _callerCancellationToken = callerCancellationToken;
            _upStreamChannel = upStreamChannel;
            _headerEncoder = headerEncoder;
            _response = new H2Message(headerEncoder.Decoder, StreamIdentifier, MemoryPool<byte>.Shared, this);
            RemoteWindowSize = new WindowSizeHolder(globalSetting.OverallWindowSize, streamIdentifier);
            OverallRemoteWindowSize = overallWindowSizeHolder; 
            
            _globalSetting = globalSetting;
            _parser = parser;

            _receptionBufferContainer = MemoryPool<byte>.Shared.RendExact(16 * 1024);

            _dataReceptionBuffer = _receptionBufferContainer.Memory;
            
            _currentStreamCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(callerCancellationToken, mainLoopCancellationToken);

            _currentStreamCancellationTokenSource.Token.Register(OnStreamHaltRequest);
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
                

                _responseHeaderReady.TryEnd(rstStreamException);
                _responseBodyComplete.TryEnd(rstStreamException);

                return; 
            }

            if (_callerCancellationToken.IsCancellationRequested)
            {
                // The caller cancelled the request 
                _responseHeaderReady.TryEnd();
                _responseBodyComplete.TryEnd();
                return;
            }

            // In case of exception in the main loop, we ensure that wait tasks for the caller are 
            // properly interrupt with exception

            var goAwayException = _parent.GoAwayException;

            _responseHeaderReady.TryEnd(goAwayException);
            _responseBodyComplete.TryEnd(goAwayException);
        }
        
        public int StreamIdentifier { get;  }

        public byte StreamPriority { get; private set; }

        public int StreamDependency { get; private set; }

        public bool Exclusive { get; private set; }

        public WindowSizeHolder RemoteWindowSize { get;  }
        
        public WindowSizeHolder OverallRemoteWindowSize { get;  }
        
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

            _responseBodyComplete.TrySetResult(null);
            _parent.NotifyDispose(this);
            _exchange.ExchangeCompletionSource
                .TrySetException(new ExchangeException($"Receive  RST : {errorCode} from server"));
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
                .ContinueWith(t => _exchange.Metrics.TotalSent += readyToBeSent.Length, _callerCancellationToken);
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
                    
                    bookedSize = await BookWindowSize(bookedSize, _currentStreamCancellationTokenSource.Token)
                        .ConfigureAwait(false);
                    
                    if (bookedSize == 0)
                        throw new TaskCanceledException("Stream cancellation request");

                    var dataFramePayloadLength = await requestBodyStream
                        .ReadAsync(_dataReceptionBuffer.Slice(9, bookedSize), 
                            _currentStreamCancellationTokenSource.Token)
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

        public async Task<H2Message> ProcessResponse(CancellationToken cancellationToken)
        {
            await _responseHeaderReady.Task.ConfigureAwait(false);

            return _response;
        }

        internal void ReceiveHeaderFragmentFromConnection(HeadersFrame headersFrame)
        {
            _exchange.Metrics.TotalReceived += headersFrame.BodyLength;
            _exchange.Metrics.ResponseHeaderStart = ITimingProvider.Default.Instant();

            if (headersFrame.EndStream)
            {
                _noBodyStream = true; 
            }

            ReceiveHeaderFragmentFromConnection(headersFrame.Data, headersFrame.EndHeaders);
        }

        internal void ReceiveHeaderFragmentFromConnection(ContinuationFrame continuationFrame)
        {
            _exchange.Metrics.TotalReceived += continuationFrame.BodyLength;
            ReceiveHeaderFragmentFromConnection(continuationFrame.Data, continuationFrame.EndHeaders);
        }

        private void ReceiveHeaderFragmentFromConnection(ReadOnlyMemory<byte> buffer, bool last)
        {
            if (last)
            {
                _exchange.Metrics.ResponseHeaderEnd = ITimingProvider.Default.Instant();
            }

            _response.PostResponseHeader(buffer, last);

            _exchange.Response.Header = new ResponseHeader(
            _response.Header.AsMemory(), true, _parser);

            if (last)
            {
                _responseHeaderReady.SetResult(null);

                if (_noBodyStream)
                {
                    _response.PostResponseBodyFragment(Memory<byte>.Empty, true);

                    _responseBodyComplete.TrySetResult(null);
                    _exchange.ExchangeCompletionSource.TrySetResult(false);
                    _parent.NotifyDispose(this);
                }

            }

        }

        private bool _firstBodyFragment = true; 
        private bool _receivedEndStream = false; 

        public void ReceiveBodyFragmentFromConnection(ReadOnlyMemory<byte> buffer, bool endStream)
        {
            if (!_receivedEndStream && endStream)
            {
                _receivedEndStream = true;
            }

            if (_firstBodyFragment)
            {
                _exchange.Metrics.ResponseBodyStart = ITimingProvider.Default.Instant();
                _firstBodyFragment = false; 
            }

            if (endStream)
            {
                _exchange.Metrics.ResponseBodyEnd = ITimingProvider.Default.Instant();
            }

            _exchange.Metrics.TotalReceived += buffer.Length;

            _response.PostResponseBodyFragment(buffer, endStream);

            if (endStream)
            {
                _responseBodyComplete.TrySetResult(null);
                _parent.NotifyDispose(this);
                _exchange.ExchangeCompletionSource.TrySetResult(false);
            }
        }
        internal void OnDataConsumedByCaller(int dataSize)
        {
            if (dataSize > 0)
            {
                SendWindowUpdate(dataSize, StreamIdentifier);
                SendWindowUpdate(dataSize, 0);
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

        private void OnError(Exception ex)
        {
            _parent.NotifyDispose(this);
        }

        public override string ToString()
        {
            return $"Stream Id : {StreamIdentifier} : {_responseHeaderReady.Task.Status} : {_responseBodyComplete.Task.Status}"; 
        }

        private bool _disposed = false; 

        public void Dispose()
        {
            _disposed = true;

            if (!_receivedEndStream)
            {
                _response.ParentHasDisposed();
            }

            RemoteWindowSize?.Dispose();
            OverallRemoteWindowSize?.Dispose();
            _currentStreamCancellationTokenSource.Dispose();
            _receptionBufferContainer?.Dispose();
        }
    }
    
}