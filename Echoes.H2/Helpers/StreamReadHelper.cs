using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2.Helpers
{
    public static class StreamReadHelper
    {
        public static void ReadExact(this Stream origin, Span<byte> span)
        {
            int readen = 0;
            int currentIndex = 0;
            int remain = span.Length; 

            while (readen < span.Length)
            {
                var currentReaden = origin.Read(span.Slice(currentIndex, remain));

                if (currentReaden <= 0)
                    throw new EndOfStreamException($"Stream does not have {span.Length} octets");

                currentIndex += currentReaden; 
                remain -= currentReaden; 
                readen += (currentReaden); 
            }
        }

        public static void ReadExact(this Stream origin, byte[] buffer, int offset, int length)
        {
            int readen = 0;
            int currentIndex = offset;
            int remain = length;
            
            while (readen < length)
            {
                var currentReaden = origin.Read(buffer, currentIndex, remain);

                if (currentReaden <= 0)
                    throw new EndOfStreamException($"Stream does not have {length} octets");

                currentIndex += currentReaden; 
                remain -= currentReaden; 
                readen += (currentReaden); 
            }
        }
        public static async Task ReadExactAsync(this Stream origin, byte[] buffer, int offset, int length, CancellationToken cancellationToken)
        {
            int readen = 0;
            int currentIndex = offset;
            int remain = length; 

            while (readen < length)
            {
                var currentReaden = await origin.ReadAsync(buffer, currentIndex, remain, cancellationToken).ConfigureAwait(false);

                if (currentReaden <= 0)
                    throw new EndOfStreamException($"Stream does not have {length} octets");

                currentIndex += currentReaden; 
                remain -= currentReaden; 
                readen += (currentReaden); 
            }
        }
        public static async Task ReadExactAsync(this Stream origin, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            int readen = 0;
            int currentIndex = 0;
            int remain = buffer.Length; 

            while (readen < buffer.Length)
            {
                var currentReaden = await origin.ReadAsync(buffer.Slice(currentIndex, remain), cancellationToken).ConfigureAwait(false);

                if (currentReaden <= 0)
                    throw new EndOfStreamException($"Stream does not have {buffer.Length} bytes");

                currentIndex += currentReaden; 
                remain -= currentReaden; 
                readen += (currentReaden); 
            }
        }
    }

    public static class IEnumerableExtensions
    {
        public static IEnumerable<T> Tap<T>(this IEnumerable<T> list, Action<T> todo)
        {
            foreach (var element in list)
            {
                todo(element); 
                yield return element; 
            }
        }
    }
}