// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Misc
{
    /// <summary>
    /// Read an inner stream up to max length
    /// </summary>
    public class ContentBoundStream : Stream
    {
        public long MaxLength { get; }

        private readonly Stream _innerStream;

        public long TotalWritten { get; private set; }

        public long TotalRead { get; private set; }

        public ContentBoundStream(Stream innerStream, long maxLength)
        {
            MaxLength = maxLength;
            _innerStream = innerStream;
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var minCount = (int) Math.Min(count, MaxLength - TotalRead);

            if (minCount == 0)
                return 0;

            var read = _innerStream.Read(buffer, offset, minCount);

            TotalRead += read;

            return read;
        }
        
        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer, offset, count);
            TotalWritten += count;
        }

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;

        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        public Stream InnerStream => _innerStream;

        public override int Read(Span<byte> buffer)
        {
            var res = _innerStream.Read(buffer);
            TotalRead += res;

            return res;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken)
                .ConfigureAwait(false); 
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            var minCount = (int)Math.Min(buffer.Length, MaxLength - TotalRead);

            if (minCount == 0)
                return 0;

            var res = await _innerStream.ReadAsync(buffer.Slice(0, minCount), cancellationToken)
                .ConfigureAwait(false);

            TotalRead += res;

            return res;
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _innerStream.Write(buffer);
            TotalWritten += buffer.Length;
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _innerStream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            TotalWritten += count;
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            await _innerStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            TotalWritten += buffer.Length;

        }

        public override void WriteByte(byte value)
        {
            _innerStream.WriteByte(value);
            TotalWritten += 1;
        }
    }
}