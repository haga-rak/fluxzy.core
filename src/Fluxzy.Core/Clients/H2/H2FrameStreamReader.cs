// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Clients.H2
{
    /// <summary>
    ///     Reads consecutive H2 frames from a single underlying <see cref="Stream" /> using a private
    ///     buffer it owns. Amortizes <see cref="Stream.ReadAsync(Memory{byte}, CancellationToken)" /> calls
    ///     across frames: a single network read can satisfy multiple small frames, and the common path
    ///     completes synchronously as a <see cref="ValueTask{TResult}" /> without allocating a state machine.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>Lifetime contract.</b> The <see cref="H2FrameReadResult" /> returned from
    ///         <see cref="ReadNextFrameAsync" /> exposes a <see cref="ReadOnlyMemory{T}" /> that points into
    ///         the reader's owned buffer. It is valid only until the next call to
    ///         <see cref="ReadNextFrameAsync" /> on the same reader. Callers must copy the body bytes out
    ///         (or finish reading from the slice) before requesting the next frame.
    ///     </para>
    ///     <para>
    ///         Not thread-safe. Intended to be driven by a single read loop per connection.
    ///     </para>
    /// </remarks>
    internal sealed class H2FrameStreamReader : IDisposable
    {
        private readonly Stream _stream;
        private readonly int _maxFrameSize;
        private readonly IMemoryOwner<byte> _owner;
        private readonly Memory<byte> _buffer;

        // Unread window: bytes in [_start, _end) have been read from the stream
        // but not yet returned to the caller as a parsed frame.
        private int _start;
        private int _end;

        public H2FrameStreamReader(Stream stream, int maxFrameSize)
        {
            _stream = stream;
            _maxFrameSize = maxFrameSize;

            // Two full max-size frames (header + body) of headroom, so a typical read after
            // a compaction always has room to land a full frame in one shot.
            var bufferSize = (maxFrameSize + 9) * 2;
            _owner = MemoryPool<byte>.Shared.Rent(bufferSize);
            _buffer = _owner.Memory.Slice(0, bufferSize);
        }

        public ValueTask<H2FrameReadResult> ReadNextFrameAsync(CancellationToken cancellationToken)
        {
            // Fast path: a complete frame is already buffered from a previous read.
            var available = _end - _start;

            if (available >= 9) {
                var header = new H2Frame(_buffer.Span.Slice(_start, 9));

                if (header.BodyLength > _maxFrameSize)
                    ThrowFrameTooLarge(header.BodyLength);

                var total = 9 + header.BodyLength;

                if (available >= total) {
                    var body = _buffer.Slice(_start + 9, header.BodyLength);
                    _start += total;
                    return new ValueTask<H2FrameReadResult>(new H2FrameReadResult(header, body));
                }
            }

            return ReadNextFrameSlowAsync(cancellationToken);
        }

        private async ValueTask<H2FrameReadResult> ReadNextFrameSlowAsync(CancellationToken cancellationToken)
        {
            // Ensure at least a full header is buffered.
            while (_end - _start < 9) {
                if (!await FillAsync(cancellationToken).ConfigureAwait(false)) {
                    // Clean EOF is only valid on a frame boundary.
                    if (_end - _start == 0)
                        return default;

                    throw new EndOfStreamException("Unexpected EOF while reading H2 frame header");
                }
            }

            var header = new H2Frame(_buffer.Span.Slice(_start, 9));

            if (header.BodyLength > _maxFrameSize)
                ThrowFrameTooLarge(header.BodyLength);

            var total = 9 + header.BodyLength;

            while (_end - _start < total) {
                if (!await FillAsync(cancellationToken).ConfigureAwait(false))
                    throw new EndOfStreamException("Unexpected EOF while reading H2 frame body");
            }

            var body = _buffer.Slice(_start + 9, header.BodyLength);
            _start += total;

            return new H2FrameReadResult(header, body);
        }

        private async ValueTask<bool> FillAsync(CancellationToken cancellationToken)
        {
            // Compact when the tail cannot fit a max-size frame. With buffer = 2*(maxFrameSize+9)
            // and available < maxFrameSize+9 (any complete frame would already have been returned),
            // compaction always yields tailFree > maxFrameSize+9 afterwards.
            var tailFree = _buffer.Length - _end;

            if (tailFree < _maxFrameSize + 9)
                CompactUnreadBytes();

            var read = await _stream.ReadAsync(_buffer.Slice(_end), cancellationToken).ConfigureAwait(false);

            if (read <= 0)
                return false;

            _end += read;
            return true;
        }

        private void CompactUnreadBytes()
        {
            var available = _end - _start;

            if (available > 0 && _start > 0)
                _buffer.Slice(_start, available).CopyTo(_buffer);

            _start = 0;
            _end = available;
        }

        private static void ThrowFrameTooLarge(int bodyLength)
        {
            throw new IOException($"Received frame is too large than MaxFrameSizeAllowed ({bodyLength})");
        }

        public void Dispose()
        {
            _owner.Dispose();
        }
    }
}
