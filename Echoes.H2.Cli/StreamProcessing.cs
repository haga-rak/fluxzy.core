using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Channels;
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
        private int _fragmentReceptionOffset; 

        
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
            H2Message response,
            H2StreamSetting globalSetting, 
            WindowSizeHolder overallWindowSizeHolder,
            ArrayPool<byte> memoryPool)
        {
            StreamIdentifier = streamIdentifier;
            _parent = parent;
            _upStreamChannel = upStreamChannel;
            _headerEncoder = headerEncoder;
            _response = response;
            RemoteWindowSize = new WindowSizeHolder(globalSetting.Remote.WindowSize);
            OverallRemoteWindowSize = overallWindowSizeHolder; 

            _dataReceptionBufferbytes = memoryPool.Rent(globalSetting.ReadBufferLength);

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

        public void NotifyWindowUpdate(int windowSizeUpdateValue)
        {
            RemoteWindowSize.UpdateWindowSize(windowSizeUpdateValue);
        }

        public void ResetRequest(H2ErrorCode errorCode)
        {

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
            var writeTask = new WriteTask(readyToBeSent, StreamIdentifier, StreamPriority, StreamDependency); 
            
             await _upStreamChannel(writeTask, _overCancellationTokenSource.Token).ConfigureAwait(false);

             // Wait for request header to be sent according to priority 
             await writeTask.DoneTask.ConfigureAwait(false);

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

                    await _upStreamChannel(
                        new WriteTask(_dataReceptionBuffer.Slice(0, 9 + readenLength ),
                            StreamIdentifier, StreamPriority, StreamDependency)
                        , _overCancellationTokenSource.Token).ConfigureAwait(false);
                }

            }
        }

        public async Task<H2Message> ProcessResponse(CancellationToken cancellationToken)
        {
            try
            {
                await _responseHeaderReady.Task.ConfigureAwait(false);
                
                await _responseReaden.Task.ConfigureAwait(false);

                return _response;
            }
            finally
            {
                _parent.NotifyDispose(this);
            }
        }

        public void ReceiveHeaderFragmentFromConnection(HeadersFrame headersFrame)
        {
            ReceiveHeaderFragmentFromConnection(headersFrame.Data, headersFrame.EndHeaders);
        }

        public void ReceiveHeaderFragmentFromConnection(ContinuationFrame continuationFrame)
        {
            ReceiveHeaderFragmentFromConnection(continuationFrame.Data, continuationFrame.EndHeaders);
        }

        private void ReceiveHeaderFragmentFromConnection(ReadOnlyMemory<byte> buffer, bool last)
        {
            _response.PostRequestHeader(buffer, last);

            if (last)
            {
                _responseHeaderReady.SetResult(_headerBuffer);
            }
        }

        public async Task ReceiveBodyFragmentFromConnection(ReadOnlyMemory<byte> buffer, bool enStream)
        {
            buffer.CopyTo(_dataReceptionBuffer.Slice(_fragmentReceptionOffset));
            _fragmentReceptionOffset += buffer.Length;

            await _response.PostRequestBodyFragment(buffer, enStream, _overCancellationTokenSource.Token).ConfigureAwait(false);

            NotifyWindowUpdate(_fragmentReceptionOffset);

            _fragmentReceptionOffset = 0;

            if (enStream)
            {
                _responseReaden.SetResult(null);
            }
        }
        
        private void OnError(Exception ex)
        {

            _parent.NotifyDispose(this);

        }

        public void Dispose()
        {
            _memoryPool.Return(_dataReceptionBufferbytes);
            _parent.NotifyDispose(this);
            RemoteWindowSize?.Dispose();
            OverallRemoteWindowSize?.Dispose();
            _overCancellationTokenSource.Dispose();
        }
    }
}