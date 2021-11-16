using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2.Cli.Helpers
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
                    throw new InvalidOperationException($"Stream does not have {span.Length} octets");

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
                    throw new InvalidOperationException($"Stream does not have {length} octets");

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
                    throw new InvalidOperationException($"Stream does not have {length} octets");

                currentIndex += currentReaden; 
                remain -= currentReaden; 
                readen += (currentReaden); 
            }
        }
    }
}