using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.Helpers
{
    public static class StreamReadHelper
    {
        public static async ValueTask<bool> ReadExactAsync(this Stream origin, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            int readen = 0;
            int currentIndex = 0;
            int remain = buffer.Length; 

            while (readen < buffer.Length)
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;

                var currentRead = await origin.ReadAsync(buffer.Slice(currentIndex, remain), cancellationToken).ConfigureAwait(false);

                if (currentRead <= 0)
                    return false; 

                currentIndex += currentRead; 
                remain -= currentRead; 
                readen += (currentRead); 
            }

            return true; 
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