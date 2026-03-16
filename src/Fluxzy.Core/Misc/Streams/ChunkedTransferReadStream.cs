// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H2.Encoder;

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

        /// <summary>
        ///     Trailer fields parsed after the final 0-length chunk (HTTP/1.1 chunked trailers).
        /// </summary>
        public List<HeaderField>? Trailers { get; private set; }

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
                    // Final chunk — parse trailers or consume terminating CRLF
                    if (!_closeOnDone)
                    {
                        await ParseTrailersAsync(cancellationToken).ConfigureAwait(false);
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
            var buffer = new Memory<byte>(bufferBinary, offset, count);

            if (_nextChunkSize == 0)
            {
                Span<byte> textBufferBytes = _lengthHolderBytes;
                Span<char> textBufferChars = _lengthHolderChar;
                Span<byte> singleByte = _singleByte;

                var textCount = 0;

                // Read chunk size until CR
                while (_innerStream.Read(singleByte) > 0)
                {
                    var b = singleByte[0];

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
                        while (_innerStream.Read(singleByte) > 0 && singleByte[0] != 0x0D) { }
                        break;
                    }

                    if (textCount >= 16)
                    { // Max hex digits for long
                        throw new IOException("Error while reading chunked stream: Chunk size too large.");
                    }

                    textBufferBytes[textCount++] = b;
                }

                // Skip LF after CR
                if (_innerStream.Read(singleByte) <= 0)
                {
                    throw new EndOfStreamException("Unexpected EOF after chunk size CR");
                }

                if (singleByte[0] != 0x0A)
                {
                    throw new IOException("Expected LF after CR in chunk size line");
                }

                if (textCount == 0)
                {
                    throw new IOException("Error while reading chunked stream: Empty chunk size.");
                }

                Encoding.ASCII.GetChars(textBufferBytes.Slice(0, textCount), textBufferChars);

                if (!long.TryParse(textBufferChars.Slice(0, textCount),
                        NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                        out var chunkSize) || chunkSize < 0)
                {
                    throw new IOException(
                        $"Error while reading chunked stream: Invalid chunk size: {new string(textBufferChars.Slice(0, textCount))}.");
                }

                if (chunkSize == 0)
                {
                    // Final chunk — parse trailers or consume terminating CRLF
                    if (!_closeOnDone)
                    {
                        ParseTrailers();
                    }
                    return 0;
                }

                _nextChunkSize = chunkSize;
            }

            var nextBlockToRead = (int)Math.Min(_nextChunkSize, buffer.Length);
            var read = _innerStream.Read(buffer.Slice(0, nextBlockToRead).Span);

            if (read <= 0)
            {
                throw new EndOfStreamException(
                    $"Error while reading chunked stream: EOF before receiving {_nextChunkSize} bytes.");
            }

            _nextChunkSize -= read;

            if (_nextChunkSize == 0)
            {
                // Read trailing CRLF after chunk data
                _innerStream.ReadExact(new Span<byte>(_lengthHolderBytes, 0, 2));
            }

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

        /// <summary>
        ///     Reads trailer headers (or just the terminating CRLF) after the final 0-length chunk.
        ///     Format: "name: value\r\n" lines terminated by an empty "\r\n" line.
        /// </summary>
        private async ValueTask ParseTrailersAsync(CancellationToken ct)
        {
            List<HeaderField>? trailerList = null;
            var lineBuilder = new StringBuilder();

            while (true)
            {
                if (!await _innerStream.ReadExactAsync(_singleByte, ct).ConfigureAwait(false))
                    break;

                var b = _singleByte[0];

                if (b == 0x0D) // CR
                {
                    // Read LF
                    await _innerStream.ReadExactAsync(_singleByte, ct).ConfigureAwait(false);

                    if (lineBuilder.Length == 0)
                        break; // Empty line = end of trailers (or no trailers at all)

                    var line = lineBuilder.ToString();
                    lineBuilder.Clear();

                    var colonIdx = line.IndexOf(':');

                    if (colonIdx > 0)
                    {
                        trailerList ??= new List<HeaderField>();
                        trailerList.Add(new HeaderField(
                            line.Substring(0, colonIdx).Trim(),
                            line.Substring(colonIdx + 1).Trim()));
                    }
                }
                else if (b != 0x0A) // skip stray LFs
                {
                    lineBuilder.Append((char)b);
                }
            }

            if (trailerList != null)
                Trailers = trailerList;
        }

        /// <summary>
        ///     Sync version of trailer parsing.
        /// </summary>
        private void ParseTrailers()
        {
            List<HeaderField>? trailerList = null;
            var lineBuilder = new StringBuilder();
            Span<byte> single = _singleByte;

            while (true)
            {
                if (_innerStream.Read(single) <= 0)
                    break;

                var b = single[0];

                if (b == 0x0D) // CR
                {
                    _innerStream.ReadExact(single); // LF

                    if (lineBuilder.Length == 0)
                        break;

                    var line = lineBuilder.ToString();
                    lineBuilder.Clear();

                    var colonIdx = line.IndexOf(':');

                    if (colonIdx > 0)
                    {
                        trailerList ??= new List<HeaderField>();
                        trailerList.Add(new HeaderField(
                            line.Substring(0, colonIdx).Trim(),
                            line.Substring(colonIdx + 1).Trim()));
                    }
                }
                else if (b != 0x0A)
                {
                    lineBuilder.Append((char)b);
                }
            }

            if (trailerList != null)
                Trailers = trailerList;
        }
    }
}