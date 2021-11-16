using System;

namespace Echoes.H2.Cli
{
    public interface IHeaderEncoder
    {
        /// <summary>
        /// Apply header + hpack to headerbuffer 
        /// </summary>
        /// <param name="headerBuffer"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        Memory<byte> Encode(Memory<byte> headerBuffer, Memory<byte> buffer); 

        /// <summary>
        /// Remove hpack 
        /// </summary>
        /// <param name="headerBuffer"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        Memory<byte> Decode(Memory<byte> headerBuffer, Memory<byte> buffer); 
    }
}