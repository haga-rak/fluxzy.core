using System;
using Echoes.Encoding.HPack;

namespace Echoes.H2.Cli
{
    internal interface IHeaderEncoder
    {

        HPackEncoder Encoder { get; }

        HPackDecoder Decoder { get; }

        /// <summary>
        /// Apply header + hpack to headerbuffer 
        /// </summary>
        /// <param name="encodingJob"></param>
        /// <param name="destinationBuffer"></param>
        /// <returns></returns>
        ReadOnlyMemory<byte> Encode(HeaderEncodingJob encodingJob, Memory<byte> destinationBuffer); 

        /// <summary>
        /// Remove hpack 
        /// </summary>
        /// <param name="encodedBuffer"></param>
        /// <param name="destinationBuffer"></param>
        /// <returns></returns>
        ReadOnlyMemory<char> Decode(ReadOnlyMemory<byte> encodedBuffer, Memory<char> destinationBuffer);
    }

    public readonly ref struct HeaderEncodingJob
    {
        public HeaderEncodingJob(ReadOnlyMemory<char> data, int streamIdentifier, int streamDependency)
        {
            Data = data;
            StreamIdentifier = streamIdentifier;
            StreamDependency = streamDependency;
        }

        public ReadOnlyMemory<char> Data { get;  }

        public int StreamIdentifier { get;  }

        public int StreamDependency { get;  }
    }


    public class HeaderEncoder : IHeaderEncoder
    {
        private readonly HPackEncoder _hpackEncoder;
        private readonly HPackDecoder _hpackDecoder;
        private readonly H2StreamSetting _streamSetting;

        public HeaderEncoder(
            HPackEncoder hpackEncoder,
            HPackDecoder hpackDecoder,
            H2StreamSetting streamSetting)
        {
            _hpackEncoder = hpackEncoder;
            _hpackDecoder = hpackDecoder;
            _streamSetting = streamSetting;
        }

        public HPackEncoder Encoder => _hpackEncoder;

        public HPackDecoder Decoder => _hpackDecoder;

        public ReadOnlyMemory<byte> Encode(HeaderEncodingJob encodingJob, Memory<byte> destinationBuffer)
        {
            Span<byte> buffer = stackalloc byte[_streamSetting.Remote.MaxHeaderLine];
            var encodedHeader = _hpackEncoder.Encode(encodingJob.Data, buffer);

            var res = Packetizer.PacketizeHeader(encodedHeader, destinationBuffer.Span, encodingJob.StreamIdentifier,
                (int) _streamSetting.Remote.MaxFrameSize, encodingJob.StreamDependency);

            return destinationBuffer.Slice(0, res.Length);
        }

        public ReadOnlyMemory<char> Decode(ReadOnlyMemory<byte> encodedBuffer, Memory<char> destinationBuffer)
        {
            var res = _hpackDecoder.Decode(encodedBuffer.Span, destinationBuffer.Span);
            return destinationBuffer.Slice(0, res.Length);
        }
    }
}