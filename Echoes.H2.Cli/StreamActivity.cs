using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Echoes.H2.Cli
{
    internal class StreamActivity: IDisposable
    {
        private readonly StreamPool _parent;
        private readonly CancellationToken _connectionToken;
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
        

        public StreamActivity(
            int streamIdentifier,
            StreamPool parent, 
            CancellationToken connectionToken,
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
            _connectionToken = connectionToken;
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

        public ValueTask WriteHeader(Memory<byte> headerBuffer, CancellationToken cancellationToken)
        {
             return _upStreamChannel(_headerEncoder.Encode(headerBuffer, _dataReceptionBuffer), cancellationToken);
        }
        
        public async ValueTask WriteBody(Stream t, CancellationToken cancellationToken)
        {
            while (true)
            {
                var bodyLength = await t.ReadAsync(_dataReceptionBuffer.Slice(9, _maxPacketSize), cancellationToken).ConfigureAwait(false);

                if (bodyLength == 0)
                {
                    // No more data to read 
                    return; 
                }

                // Allocate a WindowSize from current stream and overall stream 
                if (!await BookWindowSize(bodyLength, cancellationToken).ConfigureAwait(false))
                    throw new OperationCanceledException("Stream cancellation request", cancellationToken); 

                H2Frame.BuildDataFrameHeader(bodyLength, StreamIdentifier).Write(_dataReceptionBuffer.Span);

                await _upStreamChannel(_dataReceptionBuffer.Slice(0, bodyLength + 9), cancellationToken).ConfigureAwait(false);
            }
        }


        public void NotifyWindowUpdate(int windowSizeUpdateValue)
        {

        }

        public void ReceiveHeaderFragmentFromConnection(Memory<byte> buffer, bool last, CancellationToken cancellationToken)
        {
            buffer.CopyTo(_headerBuffer.Slice(_readHeaderBufferIndex));
            _readHeaderBufferIndex += buffer.Length;

            if (last)
            {
                _responseHeaderReady.SetResult(null);
            }
        }

        public async Task ReceiveBodyFragmentFromConnection(Memory<byte> buffer, bool enStream, CancellationToken cancellationToken)
        {
            buffer.CopyTo(_dataReceptionBuffer.Slice(_fragmentReceptionOffset));
            _fragmentReceptionOffset += buffer.Length;

            await _response.PostRequestBodyFragment(buffer, enStream, cancellationToken).ConfigureAwait(false);

            NotifyWindowUpdate(_fragmentReceptionOffset);

            _fragmentReceptionOffset = 0;


            if (enStream)
            {
                _responseReaden.SetResult(null);
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

        private void OnError(Exception ex)
        {

            _parent.NotifyDispose(this);

        }

        public void Dispose()
        {
            _parent.NotifyDispose(this);
            RemoteWindowSize?.Dispose();
            OverallRemoteWindowSize?.Dispose();
        }
    }
}