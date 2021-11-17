using System;
using System.IO;

namespace Echoes.H2.Cli
{
    public interface IHeaderEncoder
    {
        /// <summary>
        /// Apply header + hpack to headerbuffer 
        /// </summary>
        /// <param name="decodedBuffer"></param>
        /// <param name="destinationBuffer"></param>
        /// <returns></returns>
        Memory<byte> Encode(Memory<byte> decodedBuffer, Memory<byte> destinationBuffer); 

        /// <summary>
        /// Remove hpack 
        /// </summary>
        /// <param name="encodedBuffer"></param>
        /// <param name="destinationBuffer"></param>
        /// <returns></returns>
        Memory<byte> Decode(Memory<byte> encodedBuffer, Memory<byte> destinationBuffer);

        /// <summary>
        /// Remove hpack 
        /// </summary>
        /// <param name="encodedBuffer"></param>
        /// <param name="destinationStream"></param>
        /// <returns></returns>
        Memory<byte> Decode(Memory<byte> encodedBuffer, Stream destinationStream); 
    }
}