using System;
using Echoes.H2.Encoder;

namespace Echoes.H2
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
        /// <param name="endStream"></param>
        /// <returns></returns>
        ReadOnlyMemory<byte> Encode(HeaderEncodingJob encodingJob, Memory<byte> destinationBuffer, bool endStream); 

        /// <summary>
        /// Remove hpack 
        /// </summary>
        /// <param name="encodedBuffer"></param>
        /// <param name="destinationBuffer"></param>
        /// <returns></returns>
        ReadOnlyMemory<char> Decode(ReadOnlyMemory<byte> encodedBuffer, Memory<char> destinationBuffer);
    }
}