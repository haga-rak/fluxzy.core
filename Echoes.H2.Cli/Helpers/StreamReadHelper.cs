using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2.Cli.Helpers
{
    public static class StreamReadHelper
    {
        public static async Task ReadExact(this Stream origin, byte[] buffer, int offset, int length, CancellationToken cancellationToken)
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