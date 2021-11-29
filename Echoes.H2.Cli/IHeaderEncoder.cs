using System;
using System.IO;
using Echoes.Encoding.HPack;

namespace Echoes.H2.Cli
{
    public interface IHeaderEncoder
    {
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
        Memory<byte> Decode(Memory<byte> encodedBuffer, Memory<char> destinationBuffer);

        /// <summary>
        /// Remove hpack 
        /// </summary>
        /// <param name="encodedBuffer"></param>
        /// <param name="destinationStream"></param>
        /// <returns></returns>
        Memory<byte> Decode(Memory<byte> encodedBuffer, Stream destinationStream); 
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
        private readonly H2StreamSetting _streamSetting;

        public HeaderEncoder(HPackEncoder hpackEncoder, H2StreamSetting streamSetting)
        {
            _hpackEncoder = hpackEncoder;
            _streamSetting = streamSetting;
        }

        public ReadOnlyMemory<byte> Encode(HeaderEncodingJob encodingJob, Memory<byte> destinationBuffer)
        {
            Span<byte> buffer = stackalloc byte[_streamSetting.Remote.MaxHeaderLine];
            var encodedHeader = _hpackEncoder.Encode(encodingJob.Data, buffer);

            var res = Packetizer.Packetize(encodedHeader, destinationBuffer.Span, encodingJob.StreamIdentifier,
                (int) _streamSetting.Remote.MaxFrameSize, encodingJob.StreamDependency);

            return destinationBuffer.Slice(0, res.Length);
        }

        public Memory<byte> Decode(Memory<byte> encodedBuffer, Memory<char> destinationBuffer)
        {
            throw new NotImplementedException();
        }

        public Memory<byte> Decode(Memory<byte> encodedBuffer, Stream destinationStream)
        {
            throw new NotImplementedException();
        }
    }
}