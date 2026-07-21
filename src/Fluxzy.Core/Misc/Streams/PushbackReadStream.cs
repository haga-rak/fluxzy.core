// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Misc.Streams
{
    /// <summary>
    ///     Readonly stream allowing already read bytes to be pushed back and served
    ///     before the inner stream. The pushback buffer is reused across pushes so
    ///     repeated pushes never nest streams.
    /// </summary>
    public class PushbackReadStream : Stream
    {
        private readonly Stream _innerStream;

        private byte[]? _pushback;
        private int _offset;
        private int _length;
        private long _position;

        public PushbackReadStream(Stream innerStream)
        {
            _innerStream = innerStream;
        }

        public int PendingLength => _length - _offset;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position {
            get => _position;

            set
            {
                if (value != _position) {
                    throw new NotSupportedException();
                }
            }
        }

        /// <summary>
        ///     Queue bytes to be served before the inner stream. Pushed bytes were read
        ///     ahead of any pending ones, so they are served first.
        /// </summary>
        public void Push(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty) {
                return;
            }

            var pendingLength = _length - _offset;

            if (pendingLength == 0) {
                if (_pushback == null || _pushback.Length < data.Length) {
                    ReturnBuffer();
                    _pushback = ArrayPool<byte>.Shared.Rent(data.Length);
                }

                data.CopyTo(_pushback);
                _offset = 0;
                _length = data.Length;

                return;
            }

            var totalLength = data.Length + pendingLength;
            var nextBuffer = ArrayPool<byte>.Shared.Rent(totalLength);

            data.CopyTo(nextBuffer);
            _pushback.AsSpan(_offset, pendingLength).CopyTo(nextBuffer.AsSpan(data.Length));

            ReturnBuffer();

            _pushback = nextBuffer;
            _offset = 0;
            _length = totalLength;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Read(buffer.AsSpan(offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            var pendingLength = _length - _offset;

            if (pendingLength > 0) {
                var read = Math.Min(pendingLength, buffer.Length);

                _pushback.AsSpan(_offset, read).CopyTo(buffer);
                _offset += read;
                _position += read;

                return read;
            }

            var innerRead = _innerStream.Read(buffer);
            _position += innerRead;

            return innerRead;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var pendingLength = _length - _offset;

            if (pendingLength > 0) {
                var read = Math.Min(pendingLength, buffer.Length);

                _pushback.AsMemory(_offset, read).CopyTo(buffer);
                _offset += read;
                _position += read;

                return new ValueTask<int>(read);
            }

            return ReadInnerAsync(buffer, cancellationToken);
        }

        private async ValueTask<int> ReadInnerAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var read = await _innerStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            _position += read;

            return read;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        private void ReturnBuffer()
        {
            if (_pushback != null) {
                ArrayPool<byte>.Shared.Return(_pushback);
                _pushback = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) {
                _offset = 0;
                _length = 0;
                ReturnBuffer();
            }

            // Inner stream is left open, matching the previous
            // CombinedReadonlyStream(closeStreams: false) behavior

            base.Dispose(disposing);
        }
    }
}
