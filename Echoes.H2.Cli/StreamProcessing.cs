using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2.Cli
{
    internal class StreamProcessing: IDisposable
    {
        private readonly StreamPool _parent;
        private readonly UpStreamChannel _upStreamChannel;
        private readonly IHeaderEncoder _headerEncoder;
        private readonly H2Message _response;
        
        private readonly Memory<byte> _dataReceptionBuffer;
        private readonly Memory<char> _headerBuffer;
        private readonly H2StreamSetting _globalSetting;
        private readonly ArrayPool<byte> _memoryPool;
        
        private readonly TaskCompletionSource<object> _responseHeaderReady = new TaskCompletionSource<object>();
        private readonly TaskCompletionSource<object> _responseReaden = new TaskCompletionSource<object>();
        private readonly CancellationTokenSource _overCancellationTokenSource;
        private readonly byte[] _dataReceptionBufferbytes;
        
        public StreamProcessing(
            int streamIdentifier,
            StreamPool parent, 
            CancellationToken remoteCancellationToken,
            CancellationToken localCancellationToken,
            UpStreamChannel upStreamChannel,
            IHeaderEncoder headerEncoder,
            H2StreamSetting globalSetting, 
            WindowSizeHolder overallWindowSizeHolder,
            ArrayPool<byte> memoryPool)
        {
            StreamIdentifier = streamIdentifier;
            _parent = parent;
            _upStreamChannel = upStreamChannel;
            _headerEncoder = headerEncoder;
            _response = new H2Message(headerEncoder.Decoder, StreamIdentifier, MemoryPool<byte>.Shared, this);
            RemoteWindowSize = new WindowSizeHolder(globalSetting.Remote.WindowSize);
            OverallRemoteWindowSize = overallWindowSizeHolder; 

            _dataReceptionBufferbytes = memoryPool.Rent(globalSetting.Local.WindowSize);

            _dataReceptionBuffer = _dataReceptionBufferbytes;
            _globalSetting = globalSetting;
            _memoryPool = memoryPool;


            _overCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(remoteCancellationToken, localCancellationToken); 
        }

        public int StreamIdentifier { get;  }

        public byte StreamPriority { get; private set; }

        public int StreamDependency { get; private set; }

        public bool Exclusive { get; private set; }

        public WindowSizeHolder RemoteWindowSize { get;  }
        
        public WindowSizeHolder OverallRemoteWindowSize { get;  }
        
        private async ValueTask<bool> BookWindowSize(int minimumBodyLength, CancellationToken cancellationToken)
        {
            if (!await RemoteWindowSize.BookWindowSize(minimumBodyLength, cancellationToken).ConfigureAwait(false))
                return false;

            if (!await OverallRemoteWindowSize.BookWindowSize(minimumBodyLength, cancellationToken).ConfigureAwait(false))
                return false;

            return true;
        }

        public void NotifyRemoteWindowUpdate(int windowSizeUpdateValue)
        {
            RemoteWindowSize.UpdateWindowSize(windowSizeUpdateValue);
        }

        public void ResetRequest(H2ErrorCode errorCode)
        {
            Logger.WriteLine($"RstStream : Error code {errorCode}");
        }

        public void SetPriority(PriorityFrame priorityFrame)
        {
            StreamDependency = priorityFrame.StreamDependency;
            StreamPriority = priorityFrame.Weight;
            Exclusive = priorityFrame.Exclusive; 
        }

        public async  ValueTask ProcessRequest(ReadOnlyMemory<char> headerBuffer, Stream requestBodyStream)
        {
            var readyToBeSent = _headerEncoder.Encode(new HeaderEncodingJob(headerBuffer, StreamIdentifier, StreamDependency), _dataReceptionBuffer);
            var writeTask = new WriteTask(H2FrameType.Headers, StreamIdentifier, StreamPriority, StreamDependency, readyToBeSent);

            _upStreamChannel(ref writeTask);

            await writeTask.DoneTask.ConfigureAwait(false); 

             // Wait for request header to be sent according to priority 

            if (requestBodyStream != null)
            {
                while (true)
                {
                    var requestedSize = _globalSetting.Remote.MaxFrameSize - 9;

                    var readenLength = await requestBodyStream.ReadAsync(_dataReceptionBuffer.Slice(9, requestedSize), _overCancellationTokenSource.Token).ConfigureAwait(false);

                    if (readenLength == 0)
                    {
                        // No more request data body to send 
                        return;
                    }

                    if (!await BookWindowSize(readenLength, _overCancellationTokenSource.Token).ConfigureAwait(false))
                        throw new OperationCanceledException("Stream cancellation request", _overCancellationTokenSource.Token);

                    var endStream = readenLength < requestedSize;

                    new DataFrame(endStream ? HeaderFlags.EndStream : HeaderFlags.None, readenLength,
                        StreamIdentifier).Write(_dataReceptionBuffer.Span);

                    var writeTaskBody = new WriteTask(H2FrameType.Data, StreamIdentifier, StreamPriority, StreamDependency, _dataReceptionBuffer.Slice(0, 9 + readenLength));

                    _upStreamChannel(ref writeTaskBody);
                    
                    NotifyRemoteWindowUpdate(-readenLength);
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
            ReceiveHeaderFragmentFromConnection(headersFrame.Data, headersFrame.EndHeaders);
        }

        internal void ReceiveHeaderFragmentFromConnection(ContinuationFrame continuationFrame)
        {
            ReceiveHeaderFragmentFromConnection(continuationFrame.Data, continuationFrame.EndHeaders);
        }

        private void ReceiveHeaderFragmentFromConnection(ReadOnlyMemory<byte> buffer, bool last)
        {
            _response.PostResponseHeader(buffer, last);

            if (last)
                _responseHeaderReady.SetResult(_headerBuffer);
        }


        public void ReceiveBodyFragmentFromConnection(ReadOnlyMemory<byte> buffer, bool endStream)
        {
            // TODO : control sender of not overwhelming connection 

            if (endStream)
            {

            }

            _response.PostResponseBodyFragment(buffer, endStream);

            if (endStream)
            {
                _responseReaden.SetResult(null);
                _parent.NotifyDispose(this);
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

        public void Dispose()
        {
            _memoryPool.Return(_dataReceptionBufferbytes);
            RemoteWindowSize?.Dispose();
            OverallRemoteWindowSize?.Dispose();
            _overCancellationTokenSource.Dispose();
        }
    }
    
}