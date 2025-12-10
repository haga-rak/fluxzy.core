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
    ///     A stream that read chunked transfer encoding
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

        public override long Position
        {
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
            return await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).ConfigureAwait(false);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new())
        {
            if (_nextChunkSize == 0)
            {
                Memory<byte> textBufferBytes = _lengthHolderBytes;
                Memory<char> textBufferChars = _lengthHolderChar;
                Memory<byte> singleByte = _singleByte;

                var textCount = 0;

                // Read chunk size until CR
                while (await _innerStream.ReadAsync(singleByte, cancellationToken).ConfigureAwait(false) > 0)
                {
                    var b = singleByte.Span[0];

                    if (b == 0x0D)
                    { // CR found
                        break;
                    }

                    // Skip LF (handles the case after trailing CRLF of previous chunk data)
                    if (b == 0x0A)
                    {
                        continue;
                    }

                    // Ignore chunk extensions (everything after ';')
                    if (b == ';')
                    {
                        // Read until CR, discarding extension
                        while (await _innerStream.ReadAsync(singleByte, cancellationToken).ConfigureAwait(false) > 0
                               && singleByte.Span[0] != 0x0D) { }
                        break;
                    }

                    if (textCount >= 16)
                    { // Max hex digits for long
                        throw new IOException("Error while reading chunked stream: Chunk size too large.");
                    }

                    textBufferBytes.Span[textCount++] = b;
                }

                // Skip LF after CR
                if (!await _innerStream.ReadExactAsync(singleByte, cancellationToken).ConfigureAwait(false))
                {
                    throw new EndOfStreamException("Unexpected EOF after chunk size CR");
                }

                if (singleByte.Span[0] != 0x0A)
                {
                    throw new IOException("Expected LF after CR in chunk size line");
                }

                if (textCount == 0)
                {
                    throw new IOException("Error while reading chunked stream: Empty chunk size.");
                }

                Encoding.ASCII.GetChars(textBufferBytes.Span.Slice(0, textCount), textBufferChars.Span);

                if (!long.TryParse(textBufferChars.Slice(0, textCount).Span,
                        NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                        out var chunkSize) || chunkSize < 0)
                {
                    throw new IOException(
                        $"Error while reading chunked stream: Invalid chunk size: {textBufferChars.Slice(0, textCount).ToString()}.");
                }

                if (chunkSize == 0)
                {
                    // Final chunk - read terminating CRLF
                    if (!_closeOnDone)
                    {
                        await _innerStream.ReadExactAsync(textBufferBytes.Slice(0, 2), cancellationToken).ConfigureAwait(false);
                    }
                    return 0;
                }

                _nextChunkSize = chunkSize;
            }

            var nextBlockToRead = (int)Math.Min(_nextChunkSize, buffer.Length);
            var read = await _innerStream.ReadAsync(buffer.Slice(0, nextBlockToRead), cancellationToken).ConfigureAwait(false);

            if (read <= 0)
            {
                throw new EndOfStreamException(
                    $"Error while reading chunked stream: EOF before receiving {_nextChunkSize} bytes.");
            }

            _nextChunkSize -= read;

            if (_nextChunkSize == 0)
            {
                // Read trailing CRLF after chunk data
                await _innerStream.ReadExactAsync(new Memory<byte>(_lengthHolderBytes, 0, 2), cancellationToken).ConfigureAwait(false);
            }

            return read;
        }

        public override int Read(byte[] bufferBinary, int offset, int count)
        {
            throw new NotSupportedException("This stream is async only");
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