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
        private readonly CancellationToken _remoteCancellationToken;
        private readonly CancellationToken _localCancellationToken;
        private readonly UpStreamChannel _upStreamChannel;
        private readonly IHeaderEncoder _headerEncoder;
        private readonly H2Message _response;
        private readonly int _maxPacketSize;
        
        private readonly Memory<byte> _dataReceptionBuffer;
        private readonly Memory<byte> _headerBuffer;
        private readonly H2StreamSetting _globalSetting;
        private readonly ArrayPool<byte> _memoryPool;
        private int _readHeaderBufferIndex = 0;
        private int _fragmentReceptionOffset; 

        
        private readonly TaskCompletionSource<Memory<byte>> _responseHeaderReady = new TaskCompletionSource<Memory<byte>>();
        private readonly TaskCompletionSource<object> _responseReaden = new TaskCompletionSource<object>();
        private readonly CancellationTokenSource _overCancellationTokenSource;
        private readonly byte[] _dataReceptionBufferbytes;
        private readonly byte[] _headerBufferBytes;


        public StreamProcessing(
            int streamIdentifier,
            StreamPool parent, 
            CancellationToken remoteCancellationToken,
            CancellationToken localCancellationToken,
            UpStreamChannel upStreamChannel,
            IHeaderEncoder headerEncoder,
            H2Message response,
            H2StreamSetting globalSetting, 
            ArrayPool<byte> memoryPool)
        {
            StreamIdentifier = streamIdentifier;
            _parent = parent;
            _remoteCancellationToken = remoteCancellationToken;
            _localCancellationToken = localCancellationToken;
            _upStreamChannel = upStreamChannel;
            _headerEncoder = headerEncoder;
            _response = response;
            RemoteWindowSize = new WindowSizeHolder(globalSetting.Remote.WindowSize);
            OverallRemoteWindowSize = new WindowSizeHolder(globalSetting.Remote.WindowSize); 

            _dataReceptionBufferbytes = memoryPool.Rent(globalSetting.ReadBufferLength);
            _headerBufferBytes = memoryPool.Rent(globalSetting.MaxHeaderSize);
            ;


            _dataReceptionBuffer = _dataReceptionBufferbytes;
            _headerBuffer = _headerBufferBytes;
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

        }

        public void ResetRequest(int errorCode)
        {

        }

        public void SetPriority(bool exclusive, int streamDependency, byte weight)
        {
            StreamDependency = streamDependency;
            StreamPriority = weight;
            Exclusive = exclusive; 
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
                    // TODO packetize here !!!!!!! 
                    var bodyLength = await requestBodyStream.ReadAsync(_dataReceptionBuffer.Slice(9, _maxPacketSize), _overCancellationTokenSource.Token).ConfigureAwait(false);

                    if (bodyLength == 0)
                    {
                        // No more request data body to send 
                        return;
                    }

                    if (!await BookWindowSize(bodyLength, _overCancellationTokenSource.Token).ConfigureAwait(false))
                        throw new OperationCanceledException("Stream cancellation request", _overCancellationTokenSource.Token);

                    H2Frame.BuildDataFrameHeader(bodyLength, StreamIdentifier).Write(_dataReceptionBuffer.Span);

                    await _upStreamChannel(
                        new WriteTask(_dataReceptionBuffer.Slice(0, bodyLength + 9),
                            StreamIdentifier, StreamPriority, StreamDependency)
                        , _overCancellationTokenSource.Token).ConfigureAwait(false);
                }

            }
        }

        public async Task<H2Message> ProcessResponse(CancellationToken cancellationToken)
        {
            try
            {
                var data = await _responseHeaderReady.Task.ConfigureAwait(false);

                data.CopyTo(_headerBuffer);

                _response.PostRequestHeader(_headerBuffer.Slice(0, data.Length));

                await _responseReaden.Task.ConfigureAwait(false);

                return _response;
            }
            finally
            {
                _parent.NotifyDispose(this);
            }
        }

        public void ReceiveHeaderFragmentFromConnection(ReadOnlyMemory<byte> buffer, bool last)
        {
            buffer.CopyTo(_headerBuffer.Slice(_readHeaderBufferIndex));
            _readHeaderBufferIndex += buffer.Length;

            if (last)
            {
                _responseHeaderReady.SetResult(null);
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
            _memoryPool.Return(_headerBufferBytes);
            
            _parent.NotifyDispose(this);
            RemoteWindowSize?.Dispose();
            OverallRemoteWindowSize?.Dispose();
            _overCancellationTokenSource.Dispose();
        }
    }
}