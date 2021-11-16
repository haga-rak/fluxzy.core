using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Echoes.H2.Cli.IO;

namespace Echoes.H2.Cli
{
    public delegate ValueTask UpStreamChannel(Memory<byte> data, CancellationToken token); 

    internal class ActiveStream
    {
        private readonly byte [] _buffer = new byte[1024 * 16]; 
        
        private readonly UpStreamChannel _upStreamChannel;
        private readonly IHeaderEncoder _headerEncoder;
        private readonly int _maxPacketSize;

        private readonly Channel<Stream> _downStreamChannel;
        private readonly Memory<byte> _memoryBuffer;

        public ActiveStream(
            int streamIdentifier,
            UpStreamChannel upStreamChannel,
            IHeaderEncoder headerEncoder,
            WindowSizeHolder localWindowSize,
            WindowSizeHolder overallRemoteWindowSize, 
            WindowSizeHolder remoteWindowSize, 
            int maxPacketSize)
        {
            StreamIdentifier = streamIdentifier;
            _upStreamChannel = upStreamChannel;
            _headerEncoder = headerEncoder;
            _maxPacketSize = maxPacketSize;
            RemoteWindowSize = remoteWindowSize;
            OverallRemoteWindowSize = overallRemoteWindowSize;
            LocalWindowSize = localWindowSize;
            _memoryBuffer = new Memory<byte>(_buffer);
            Used = true;

            _downStreamChannel = Channel.CreateUnbounded<Stream>(new UnboundedChannelOptions()
            {
                SingleReader = true, 
                SingleWriter = true,
            }); 
        }

        public int StreamIdentifier { get;  }

        public bool Used { get; internal set; }

        public StreamStateType Type { get; internal set; } = StreamStateType.Idle;

        public WindowSizeHolder LocalWindowSize { get;  }

        public WindowSizeHolder RemoteWindowSize { get;  }

        public WindowSizeHolder OverallRemoteWindowSize { get;  }

        public ValueTask WriteHeader(Memory<byte> headerBuffer, CancellationToken cancellationToken)
        {
             return _upStreamChannel(_headerEncoder.Encode(headerBuffer, _memoryBuffer), cancellationToken);
        }

        private async ValueTask<bool> BookWindowSize(int minimumBodyLength, CancellationToken cancellationToken)
        {
            if (!await LocalWindowSize.BookWindowSize(minimumBodyLength, cancellationToken).ConfigureAwait(false))
                return false; 

            if (!await RemoteWindowSize.BookWindowSize(minimumBodyLength, cancellationToken).ConfigureAwait(false))
                return false;

            return true; 
        }

        public async ValueTask WriteBody(Stream t, CancellationToken cancellationToken)
        {
            while (true)
            {
                var bodyLength = await t.ReadAsync(_memoryBuffer.Slice(9, _maxPacketSize), cancellationToken).ConfigureAwait(false);

                if (bodyLength == 0)
                {
                    // No more data to read 
                    return; 
                }

                // Allocate a WindowSize from current stream and overall stream 
                if (!await BookWindowSize(bodyLength, cancellationToken).ConfigureAwait(false))
                    throw new OperationCanceledException("Stream cancellation request", cancellationToken); 

                H2Frame.BuildDataFrameHeader(bodyLength, StreamIdentifier).Write(_memoryBuffer.Span);

                await _upStreamChannel(_memoryBuffer.Slice(0, bodyLength + 9), cancellationToken).ConfigureAwait(false);
            }
        }

        public async ValueTask ReadBody(Stream t, CancellationToken cancellationToken)
        {
            var bodyStream = await _downStreamChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            await bodyStream.CopyToAsync(t, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask ReadHeader(Stream t, CancellationToken cancellationToken)
        {
            var bodyStream = await _downStreamChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            await bodyStream.CopyToAsync(new H2HeaderDecodeStream(t), cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask Receive(Stream stream, CancellationToken cancellationToken)
        {
            await _downStreamChannel.Writer.WriteAsync(stream, cancellationToken).ConfigureAwait(false); 
        }
        
    }
}