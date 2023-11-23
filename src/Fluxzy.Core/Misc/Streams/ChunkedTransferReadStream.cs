// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Misc.Streams
{
    /// <summary>
    /// A stream that read chunked transfer encoding
    /// </summary>
    public class ChunkedTransferReadStream : Stream
    {
        private readonly bool _closeOnDone;
        private readonly Stream _innerStream;
        private readonly byte[] _lengthHolderBytes = new byte[64];

        private readonly char[] _lengthHolderChar = new char[64];
        private readonly byte[] _singleByte = new byte[1];

        private long _nextChunkSize;

        public ChunkedTransferReadStream(Stream innerStream, bool closeOnDone)
        {
            _innerStream = innerStream;
            _closeOnDone = closeOnDone;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new InvalidOperationException();

        public override long Position {
            get => throw new InvalidOperationException();

            set => throw new InvalidOperationException();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override async Task<int> ReadAsync(
            byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            return await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new())
        {
            // Read the length of the next block 

            if (_nextChunkSize == 0) {
                Memory<byte> textBufferBytes = _lengthHolderBytes;
                Memory<char> textBufferChars = _lengthHolderChar;
                Memory<byte> singleByte = _singleByte;

                var textCount = 0;

                var chunkSizeNotDetected = true;

                while (
                    await _innerStream.ReadAsync(singleByte, cancellationToken) > 0
                    && (chunkSizeNotDetected = singleByte.Span[0] != 0XD)) {
                    if (textCount > 40)
                        throw new IOException("Error while reading chunked stream : Chunk size is larger than 40.");

                    textBufferBytes.Span[textCount++] = singleByte.Span[0];
                }

                if (textCount == 0 && !chunkSizeNotDetected) {
                    // Natural End of stream . Read the last double cr lf 
                    await _innerStream.ReadExactAsync(textBufferBytes.Slice(0, 3), cancellationToken);

                    return 0;
                }

                if (textCount == 0 || chunkSizeNotDetected) {
                    throw new IOException(
                        "Error while reading chunked stream : EOF was reached on chunked stream before reading a valid length block.");
                }

                Encoding.ASCII.GetChars(textBufferBytes.Span.Slice(0, textCount), textBufferChars.Span);

                if (!long.TryParse(textBufferChars.Slice(0, textCount).Span,
                        NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                        out var chunkSize) || chunkSize < 0) {
                    throw new IOException(
                        $"Error while reading chunked stream : Received chunk size is invalid : {textBufferChars.Slice(0, textCount).ToString()}.");
                }

                if (chunkSize == 0) {
                    if (!_closeOnDone)
                        await _innerStream.ReadExactAsync(textBufferBytes.Slice(0, 3), cancellationToken);

                    return 0;
                }

                // Skip the next CRLF 
                if (!await _innerStream.ReadExactAsync(textBufferBytes.Slice(0, 1), cancellationToken))
                    throw new EndOfStreamException("Unexpected EOF");

                _nextChunkSize = chunkSize;
            }

            var nextBlockToRead = (int) Math.Min(_nextChunkSize, buffer.Length);

            var read = await _innerStream.ReadAsync(buffer.Slice(0, nextBlockToRead), cancellationToken);

            if (read <= 0) {
                throw new EndOfStreamException(
                    $"Error while reading chunked stream : EOF was reached before receiving {_nextChunkSize} bytes of chunked data.");
            }

            _nextChunkSize -= read;

            if (_nextChunkSize == 0)
                await _innerStream.ReadExactAsync(new Memory<byte>(_lengthHolderBytes, 0, 2), cancellationToken);

            return read;
        }

        public override int Read(byte[] bufferBinary, int offset, int count)
        {
            var buffer = new Memory<byte>(bufferBinary, offset, count);

            // Read the length of the next block 

            if (_nextChunkSize == 0) {
                Memory<byte> textBufferBytes = _lengthHolderBytes;
                Memory<char> textBufferChars = _lengthHolderChar;
                Memory<byte> singleByte = _singleByte;

                var textCount = 0;

                var chunkSizeNotDetected = true;

                while (
                    _innerStream.Read(singleByte.Span) > 0
                    && (chunkSizeNotDetected = singleByte.Span[0] != 0XD)) {
                    if (textCount > 40)
                        throw new IOException("Error while reading chunked stream : Chunk size is larger than 40.");

                    textBufferBytes.Span[textCount++] = singleByte.Span[0];
                }

                if (textCount == 0 && !chunkSizeNotDetected) {
                    // Natural End of stream . Read the last double cr lf 
                    _innerStream.ReadExact(textBufferBytes.Slice(0, 3).Span);

                    return 0;
                }

                if (textCount == 0 || chunkSizeNotDetected) {
                    throw new IOException(
                        "Error while reading chunked stream : EOF was reached on chunked stream before reading a valid length block.");
                }

                Encoding.ASCII.GetChars(textBufferBytes.Span.Slice(0, textCount), textBufferChars.Span);

                if (!long.TryParse(textBufferChars.Slice(0, textCount).Span,
                        NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                        out var chunkSize) || chunkSize < 0) {
                    throw new IOException(
                        $"Error while reading chunked stream : Received chunk size is invalid : {textBufferChars.Slice(0, textCount).ToString()}.");
                }

                if (chunkSize == 0) {
                    if (!_closeOnDone)
                        _innerStream.ReadExact(textBufferBytes.Slice(0, 3).Span);

                    return 0;
                }

                // Skip the next CRLF 
                if (!_innerStream.ReadExact(textBufferBytes.Slice(0, 1).Span))
                    throw new EndOfStreamException("Unexpected EOF");

                _nextChunkSize = chunkSize;
            }

            var nextBlockToRead = (int) Math.Min(_nextChunkSize, buffer.Length);

            var read = _innerStream.Read(buffer.Slice(0, nextBlockToRead).Span);

            if (read <= 0) {
                throw new EndOfStreamException(
                    $"Error while reading chunked stream : EOF was reached before receiving {_nextChunkSize} bytes of chunked data.");
            }

            _nextChunkSize -= read;

            if (_nextChunkSize == 0)
                _innerStream.ReadExact(new Memory<byte>(_lengthHolderBytes, 0, 2).Span);

            return read;
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
    }
}
