// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Misc.Streams
{
    public static class StreamExtensions
    {
        public static async ValueTask<long> CopyDetailed(this Stream source,
            Stream destination,
            int bufferSize, Action<int> onContentCopied, CancellationToken cancellationToken)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

            try
            {
                return await source.CopyDetailed(destination, buffer, onContentCopied, cancellationToken);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public static async ValueTask<int> Drain(this Stream stream, int bufferSize = 16 * 1024, bool disposeStream = false)
        {
            var buffer = new byte[bufferSize];
            int read;
            var total = 0;

            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                total += read;
            }

            if (disposeStream)
            {
                await stream.DisposeAsync(); 
            }

            return total;
        }

        public static async ValueTask<long> CopyDetailed(this Stream source,
            Stream destination,
            byte[] buffer, Action<int> onContentCopied, CancellationToken cancellationToken)
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

        public static async ValueTask<int> ReadAtLeastAsync(this Stream origin,
            Memory<byte> buffer, int atLeastLength,
            CancellationToken cancellationToken = default)
        {
            int read = 0;
            int totalRead = 0;

            while ((read = await origin.ReadAsync(buffer, cancellationToken)) > 0)
            {
                buffer = buffer.Slice(read);

                totalRead += read;

                if (totalRead >= atLeastLength)
                    return totalRead; 
            }

            return -1; 
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
                readen += currentRead;
            }

            return true;
        }


        public static int SeekableStreamToBytes(this Stream origin, byte[] buffer)
        {
            var index = 0;
            int read;

            while (index < buffer.Length && (read = origin.Read(buffer, index, buffer.Length - index)) > 0)
            {
                index += read;
            }

            return index;

        }
    }
}