using System;
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

        private readonly Channel<Memory<byte>> _downStreamChannel;
        private readonly Memory<byte> _dataReceptionBuffer;
        private readonly Memory<byte> _headerBuffer;
        private readonly H2StreamSetting _globalSetting;
        private int _readHeaderBufferIndex = 0;
        private int _fragmentReceptionOffset; 

        
        private readonly TaskCompletionSource<Memory<byte>> _responseHeaderReady = new TaskCompletionSource<Memory<byte>>();
        private readonly TaskCompletionSource<object> _responseReaden = new TaskCompletionSource<object>();
        private readonly CancellationTokenSource _overCancellationTokenSource;


        public StreamProcessing(
            int streamIdentifier,
            StreamPool parent, 
            CancellationToken remoteCancellationToken,
            CancellationToken localCancellationToken,
            UpStreamChannel upStreamChannel,
            IHeaderEncoder headerEncoder,
            WindowSizeHolder overallRemoteWindowSize, 
            WindowSizeHolder remoteWindowSize, 
            H2Message response,
            Memory<byte> dataReceptionBuffer, 
            Memory<byte> headerBuffer,
            H2StreamSetting globalSetting,
            int maxPacketSize)
        {
            StreamIdentifier = streamIdentifier;
            _parent = parent;
            _remoteCancellationToken = remoteCancellationToken;
            _localCancellationToken = localCancellationToken;
            _upStreamChannel = upStreamChannel;
            _headerEncoder = headerEncoder;
            _response = response;
            _maxPacketSize = maxPacketSize;
            RemoteWindowSize = remoteWindowSize;
            OverallRemoteWindowSize = overallRemoteWindowSize;
            _dataReceptionBuffer = dataReceptionBuffer;
            _headerBuffer = headerBuffer;
            _globalSetting = globalSetting;

            _downStreamChannel = Channel.CreateUnbounded<Memory<byte>>(new UnboundedChannelOptions()
            {
                SingleReader = true, 
                SingleWriter = true,
            });

            _overCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(remoteCancellationToken, localCancellationToken); 
        }

        public int StreamIdentifier { get;  }

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

        public void SetPriority(bool exclusive, uint streamDependency, byte weight)
        {

        }

        public async  ValueTask ProcessRequest(Memory<byte> headerBuffer, Stream t)
        {
             await _upStreamChannel(_headerEncoder.Encode(headerBuffer, _dataReceptionBuffer), _overCancellationTokenSource.Token).ConfigureAwait(false);

             while (true)
             {
                 var bodyLength = await t.ReadAsync(_dataReceptionBuffer.Slice(9, _maxPacketSize), _overCancellationTokenSource.Token).ConfigureAwait(false);

                 if (bodyLength == 0)
                 {
                     // No more data to read 
                     return;
                 }
                 
                 if (!await BookWindowSize(bodyLength, _overCancellationTokenSource.Token).ConfigureAwait(false))
                     throw new OperationCanceledException("Stream cancellation request", _overCancellationTokenSource.Token);

                 H2Frame.BuildDataFrameHeader(bodyLength, StreamIdentifier).Write(_dataReceptionBuffer.Span);

                 await _upStreamChannel(_dataReceptionBuffer.Slice(0, bodyLength + 9), _overCancellationTokenSource.Token).ConfigureAwait(false);
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

        public void ReceiveHeaderFragmentFromConnection(Memory<byte> buffer, bool last)
        {
            buffer.CopyTo(_headerBuffer.Slice(_readHeaderBufferIndex));
            _readHeaderBufferIndex += buffer.Length;

            if (last)
            {
                _responseHeaderReady.SetResult(null);
            }
        }

        public async Task ReceiveBodyFragmentFromConnection(Memory<byte> buffer, bool enStream)
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
            _parent.NotifyDispose(this);
            RemoteWindowSize?.Dispose();
            OverallRemoteWindowSize?.Dispose();
            _overCancellationTokenSource.Dispose();
        }
    }
}