// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H2.Encoder;

namespace Fluxzy.Misc.Streams
{
    /// <summary>
    ///     A stream that write chunked transfer encoding
    /// </summary>
    /// <remarks>
    ///     Instances are not safe for concurrent use. Disposal returns staging buffers but leaves the inner stream open.
    /// </remarks>
    public class ChunkedTransferWriteStream : Stream
    {
        // A 64 KiB payload plus framing rents the 128 KiB Shared ArrayPool bucket.
        private const int MaxStagedPayloadLength = 64 * 1024;
        private static readonly byte[] ChunkTerminator =
            { (byte) '0', (byte) '\r', (byte) '\n', (byte) '\r', (byte) '\n' };

        private static readonly byte[] LineTerminator = { (byte) '\r', (byte) '\n' };
        private readonly Stream _innerStream;
        private readonly ArrayPool<byte> _arrayPool;
        private byte[]? _asyncHeaderBuffer;
        private byte[]? _writeBuffer;
        private int _writeBufferUsedLength;

        private bool _eof;

        public ChunkedTransferWriteStream(Stream innerStream)
            : this(innerStream, ArrayPool<byte>.Shared)
        {
        }

        internal ChunkedTransferWriteStream(Stream innerStream, ArrayPool<byte> arrayPool)
        {
            _innerStream = innerStream;
            _arrayPool = arrayPool;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => !_eof;

        public override long Length => throw new NotSupportedException();

        public override long Position {
            get => throw new NotSupportedException();

            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
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
            ValidateBufferArguments(buffer, offset, count);
            Write(buffer.AsSpan(offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (buffer.IsEmpty)
                return;

            if (buffer.Length > MaxStagedPayloadLength) {
                Span<byte> header = stackalloc byte[10];
                var largeHeaderLength = FormatChunkHeader(buffer.Length, header);
                _innerStream.Write(header.Slice(0, largeHeaderLength));
                _innerStream.Write(buffer);
                _innerStream.Write(LineTerminator);
                return;
            }

            var frame = GetWriteBuffer(buffer.Length);
            var headerLength = FormatChunkHeader(buffer.Length, frame);
            buffer.CopyTo(frame.AsSpan(headerLength));
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(headerLength + buffer.Length), 0x0D0A);
            var frameLength = headerLength + buffer.Length + LineTerminator.Length;
            _writeBufferUsedLength = Math.Max(_writeBufferUsedLength, frameLength);
            _innerStream.Write(frame, 0, frameLength);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).ConfigureAwait(false);
        }

        public override async ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = new())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (buffer.IsEmpty) {
                return;
            }

            if (buffer.Length > MaxStagedPayloadLength) {
                var header = _asyncHeaderBuffer ??= new byte[10];
                var largeHeaderLength = FormatChunkHeader(buffer.Length, header);
                await _innerStream.WriteAsync(
                        header.AsMemory(0, largeHeaderLength), cancellationToken)
                    .ConfigureAwait(false);
                await _innerStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                await _innerStream.WriteAsync(LineTerminator, cancellationToken).ConfigureAwait(false);
                return;
            }

            var frame = GetWriteBuffer(buffer.Length);
            var headerLength = FormatChunkHeader(buffer.Length, frame);
            buffer.CopyTo(frame.AsMemory(headerLength));
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(headerLength + buffer.Length), 0x0D0A);
            var frameLength = headerLength + buffer.Length + LineTerminator.Length;
            _writeBufferUsedLength = Math.Max(_writeBufferUsedLength, frameLength);

            await _innerStream.WriteAsync(
                    frame.AsMemory(0, frameLength), cancellationToken)
                .ConfigureAwait(false);
        }

        private byte[] GetWriteBuffer(int payloadLength)
        {
            var requiredLength = checked(payloadLength + 12);

            if (_writeBuffer == null || _writeBuffer.Length < requiredLength) {
                ReturnWriteBuffer();
                _writeBuffer = _arrayPool.Rent(requiredLength);
            }

            return _writeBuffer;
        }

        private void ReturnWriteBuffer()
        {
            if (_writeBuffer != null) {
                var writeBuffer = _writeBuffer;
                _writeBuffer = null;
                writeBuffer.AsSpan(0, _writeBufferUsedLength).Clear();
                _writeBufferUsedLength = 0;
                _arrayPool.Return(writeBuffer);
            }
        }

        private static int FormatChunkHeader(int count, Span<byte> destination)
        {
            if (!Utf8Formatter.TryFormat(count, destination, out var written, 'X'))
                throw new InvalidOperationException("Chunk header buffer too small.");

            BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(written), 0x0D0A);

            return written + 2;
        }

        public ValueTask WriteEof()
        {
            if (!_eof) {
                _eof = true;
                ReturnWriteBuffer();

                return _innerStream.WriteAsync(ChunkTerminator);
            }

            return default;
        }

        public ValueTask WriteEof(List<HeaderField>? trailers)
        {
            if (!_eof) {
                _eof = true;
                ReturnWriteBuffer();

                if (trailers != null && trailers.Count > 0) {
                    return WriteEofWithTrailersAsync(trailers);
                }

                return _innerStream.WriteAsync(ChunkTerminator);
            }

            return default;
        }

        protected override void Dispose(bool disposing)
        {
            ReturnWriteBuffer();
            base.Dispose(disposing);
        }

        private async ValueTask WriteEofWithTrailersAsync(List<HeaderField> trailers)
        {
            // Write "0\r\n" (final chunk, no data)
            await _innerStream.WriteAsync(
                new byte[] { (byte)'0', (byte)'\r', (byte)'\n' }).ConfigureAwait(false);

            // Write each trailer field as "name: value\r\n"
            foreach (var field in trailers) {
                var line = Encoding.ASCII.GetBytes($"{field.Name}: {field.Value}\r\n");
                await _innerStream.WriteAsync(line).ConfigureAwait(false);
            }

            // Terminate with empty line
            await _innerStream.WriteAsync(LineTerminator).ConfigureAwait(false);
        }
    }
}
