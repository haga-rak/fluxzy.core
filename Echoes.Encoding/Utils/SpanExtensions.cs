using System;
using System.Buffers.Binary;

namespace Echoes.Encoding.Utils
{
    internal static class SpanExtensions
    {
        /// <summary>
        /// Returns a 4 byte integer after slicing offsetBits  bits
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offsetBits"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public static ReadOnlySpan<byte> SliceBitsToNextInt32(this ReadOnlySpan<byte> buffer, int offsetBits, Span<byte> destination)
        {
            var workBuffer = buffer.Slice(offsetBits / 8);
            var sliceRemains = offsetBits % 8;

            if (sliceRemains == 0)
                return workBuffer; 

            workBuffer.Slice(0, workBuffer.Length < 8 ? workBuffer.Length : 8).CopyTo(destination);
            
            BinaryPrimitives.WriteUInt64BigEndian(destination,
                BinaryPrimitives.ReadUInt64BigEndian(destination) << sliceRemains);

            return destination.Slice(0,4);
        }
    }
}