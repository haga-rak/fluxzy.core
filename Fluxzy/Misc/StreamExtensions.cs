// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.Misc
{
    public static class StreamExtensions
    {
        public static async ValueTask<long> CopyDetailed(this Stream source,
            Stream destination,
            int bufferSize, Action<int> onContentCopied, CancellationToken cancellationToken)
        {
            return await CopyDetailed(source, destination, new byte[bufferSize], onContentCopied,
                cancellationToken);
        }

        public static async ValueTask<int> Drain(this Stream stream, int bufferSize = 16 * 1024)
        {
            var buffer = new byte[bufferSize];
            int read; 
            var total = 0; 

            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                total += read; 
            }

            return total; 
        }
        
        public static async ValueTask<long> CopyDetailed(this Stream source,
            Stream destination,
            byte [] buffer, Action<int> onContentCopied, CancellationToken cancellationToken)
        {
            long totalCopied = 0;
            int read;

            while ((read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                       .ConfigureAwait(false)) > 0)
            {
                await destination.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                onContentCopied(read);

                await destination.FlushAsync(cancellationToken);

                totalCopied += read;
            }

            return totalCopied;
        }

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
}