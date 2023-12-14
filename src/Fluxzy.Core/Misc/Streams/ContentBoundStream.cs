// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Misc.Streams
{
    /// <summary>
    ///     Convert an existing stream to a substream with max length
    /// </summary>
    public class ContentBoundStream : Stream
    {
        public ContentBoundStream(Stream innerStream, long maxLength)
        {
            MaxLength = maxLength;
            InnerStream = innerStream;
        }

        public long MaxLength { get; }

        public long TotalWritten { get; private set; }

        public long TotalRead { get; private set; }

        public override bool CanRead => InnerStream.CanRead;

        public override bool CanSeek => InnerStream.CanSeek;

        public override bool CanWrite => InnerStream.CanWrite;

        public override long Length => InnerStream.Length;

        public override long Position {
            get => InnerStream.Position;
            set => InnerStream.Position = value;
        }

        public Stream InnerStream { get; }

        public override void Flush()
        {
            InnerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var minCount = (int) Math.Min(count, MaxLength - TotalRead);

            if (minCount == 0) {
                return 0;
            }

            var read = InnerStream.Read(buffer, offset, minCount);

            TotalRead += read;

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return InnerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            InnerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            InnerStream.Write(buffer, offset, count);
            TotalWritten += count;
        }

        public override int Read(Span<byte> buffer)
        {
            var res = InnerStream.Read(buffer);
            TotalRead += res;

            return res;
        }

        public override async Task<int> ReadAsync(
            byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken)
                .ConfigureAwait(false);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new())
        {
            var minCount = (int) Math.Min(buffer.Length, MaxLength - TotalRead);

            if (minCount == 0) {
                return 0;
            }

            var res = await InnerStream.ReadAsync(buffer.Slice(0, minCount), cancellationToken)
                                       .ConfigureAwait(false);

            TotalRead += res;

            return res;
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            InnerStream.Write(buffer);
            TotalWritten += buffer.Length;
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await InnerStream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            TotalWritten += count;
        }

        public override async ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
        {
            await InnerStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            TotalWritten += buffer.Length;
        }

        public override void WriteByte(byte value)
        {
            InnerStream.WriteByte(value);
            TotalWritten += 1;
        }
    }
}
