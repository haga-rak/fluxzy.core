// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Misc.Streams
{
    public static class StreamExtensions
    {
        public static async ValueTask<long> CopyDetailed(
            this Stream source,
            Stream destination,
            int bufferSize, Action<int> onContentCopied, CancellationToken cancellationToken)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

            try {
                return await source.CopyDetailed(destination, buffer, onContentCopied, cancellationToken)
                                   .ConfigureAwait(false);
            }
            finally {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public static int Drain(this Stream stream, int bufferSize = 16 * 1024, bool disposeStream = false)
        {
            // TODO improve perf with stackalloc when bufferSize is small than an arbitrary threshold

            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

            try {
                int read;
                var total = 0;

                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0) {
                    total += read;
                }

                if (disposeStream) {
                    stream.Dispose();
                }

                return total;
            }
            finally {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public static bool DrainUntil(
            this Stream stream, long byteCount, int bufferSize = 16 * 1024, bool disposeStream = false)
        {
            // TODO improve perf with stackalloc when bufferSize is small than an arbitrary threshold

            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

            try {
                int read;
                var total = 0;

                var remaining = byteCount;

                while ((read = stream.Read(buffer, 0, (int) Math.Min(remaining, buffer.Length))) > 0) {
                    total += read;
                    remaining -= read;

                    if (remaining <= 0) {
                        break;
                    }
                }

                if (disposeStream) {
                    stream.Dispose();
                }

                return byteCount == total;
            }
            finally {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public static async ValueTask<int> DrainAsync(
            this Stream stream, int bufferSize = 16 * 1024,
            bool disposeStream = false)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

            try {
                int read;
                var total = 0;

                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0) {
                    total += read;
                }

                if (disposeStream) {
                    await stream.DisposeAsync().ConfigureAwait(false);
                }

                return total;
            }
            finally {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public static byte[] ToArrayGreedy(this Stream stream)
        {
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            return memoryStream.ToArray();
        }

        public static async Task<byte[]> ToArrayGreedyAsync(this Stream stream)
        {
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream).ConfigureAwait(false);

            return memoryStream.ToArray();
        }

        public static string ToBase64String(this Stream stream, bool dispose = false)
        {
            try {
                var array = stream.ToArrayGreedy();

                return Convert.ToBase64String(array);
            }
            finally {
                if (dispose) {
                    stream.Dispose();
                }
            }
        }

        public static long FillArray(this Stream stream, byte[] destinationArray)
        {
            var memoryStream = new MemoryStream(destinationArray);
            var read = 0;
            var totalRead = 0L;

            while ((read = stream.Read(destinationArray, 0, destinationArray.Length)) > 0) {
                memoryStream.Write(destinationArray, 0, read);
                totalRead += read;
            }

            return totalRead;
        }

        public static async ValueTask<long> CopyDetailed(
            this Stream source,
            Stream destination,
            byte[] buffer, Action<int> onContentCopied, CancellationToken cancellationToken)
        {
            long totalCopied = 0;
            int read;

            while ((read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0) {
                await destination.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                onContentCopied(read);

                await destination.FlushAsync(cancellationToken).ConfigureAwait(false);

                totalCopied += read;
            }

            return totalCopied;
        }

        public static byte[]? ReadMaxLengthOrNull(this Stream stream, int maximum)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(1024 * 16);

            try {
                int read;
                var totalRead = 0;
                var memoryStream = new MemoryStream();

                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0) {
                    memoryStream.Write(buffer, 0, read);
                    totalRead += read;

                    if (totalRead > maximum) {
                        return null;
                    }
                }

                return memoryStream.ToArray();
            }
            finally {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public static int ReadAtLeast(
            this Stream origin,
            Memory<byte> buffer, int atLeastLength,
            CancellationToken cancellationToken = default)
        {
            var read = 0;
            var totalRead = 0;

            while ((read = origin.Read(buffer.Span)) > 0) {
                buffer = buffer.Slice(read);

                totalRead += read;

                if (totalRead >= atLeastLength) {
                    return totalRead;
                }
            }

            return -1;
        }

        public static async ValueTask<int> ReadAtLeastAsync(
            this Stream origin,
            Memory<byte> buffer, int atLeastLength,
            CancellationToken cancellationToken = default)
        {
            var read = 0;
            var totalRead = 0;

            while ((read = await origin.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0) {
                buffer = buffer.Slice(read);

                totalRead += read;

                if (totalRead >= atLeastLength) {
                    return totalRead;
                }
            }

            return -1;
        }

        public static async ValueTask<bool> ReadExactAsync(
            this Stream origin, Memory<byte> buffer,
            CancellationToken cancellationToken)
        {
            var readen = 0;
            var currentIndex = 0;
            var remain = buffer.Length;

            while (readen < buffer.Length) {
                if (cancellationToken.IsCancellationRequested) {
                    return false;
                }

                var currentRead = await origin.ReadAsync(buffer.Slice(currentIndex, remain), cancellationToken)
                                              .ConfigureAwait(false);

                if (currentRead <= 0) {
                    return false;
                }

                currentIndex += currentRead;
                remain -= currentRead;
                readen += currentRead;
            }

            return true;
        }

        public static bool ReadExact(this Stream origin, Span<byte> buffer)
        {
            var readen = 0;
            var currentIndex = 0;
            var remain = buffer.Length;

            while (readen < buffer.Length) {
                var currentRead = origin.Read(buffer.Slice(currentIndex, remain));

                if (currentRead <= 0) {
                    return false;
                }

                currentIndex += currentRead;
                remain -= currentRead;
                readen += currentRead;
            }

            return true;
        }

        public static int ReadMaximum(this Stream origin, Span<byte> buffer)
        {
            var readen = 0;
            var currentIndex = 0;
            var remain = buffer.Length;

            while (readen < buffer.Length) {
                var currentRead = origin.Read(buffer.Slice(currentIndex, remain));

                if (currentRead <= 0) {
                    return readen;
                }

                currentIndex += currentRead;
                remain -= currentRead;
                readen += currentRead;
            }

            return readen;
        }

        public static int SeekableStreamToBytes(this Stream origin, byte[] buffer)
        {
            var index = 0;
            int read;

            while (index < buffer.Length && (read = origin.Read(buffer, index, buffer.Length - index)) > 0) {
                index += read;
            }

            return index;
        }

        public static string ReadToEndGreedy(this Stream stream, Encoding? encoding = null)
        {
            if (!stream.CanRead) {
                return string.Empty;
            }

            using var streamReader = new StreamReader(stream, encoding ?? Encoding.UTF8);

            return streamReader.ReadToEnd();
        }

        public static void CopyToThenDisposeDestination(this Stream src, Stream dest)
        {
            src.CopyTo(dest);
            dest.Dispose();
        }

        public static string ReadToEndWithCustomBuffer(
            this Stream stream, Encoding? encoding = null,
            int bufferSize = -1)
        {
            var memoryStream = new MemoryStream();

            var buffer = bufferSize == -1 ? new byte[1024] : new byte[bufferSize];
            encoding = encoding ?? Encoding.UTF8;

            int read;

            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0) {
                memoryStream.Write(buffer, 0, read);
            }

            return encoding.GetString(memoryStream.ToArray());
        }

        public static async Task<string> ReadToEndWithCustomBufferAsync(
            this Stream stream, Encoding? encoding = null,
            int bufferSize = -1)
        {
            var memoryStream = new MemoryStream();

            var buffer = bufferSize == -1 ? new byte[1024] : new byte[bufferSize];
            encoding ??= Encoding.UTF8;

            int read;

            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0) {
                memoryStream.Write(buffer, 0, read);
            }

            return encoding.GetString(memoryStream.ToArray());
        }
    }
}
