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
        ///     Non-async fast path: when the 2-byte read completes synchronously and yields \r\n,
        ///     no async state machine is allocated at all.
        /// </summary>
        private ValueTask ParseTrailersAsync(CancellationToken ct)
        {
            var readTask = _innerStream.ReadExactAsync(_lengthHolderBytes.AsMemory(0, 2), ct);

            if (readTask.IsCompletedSuccessfully) {
                // Synchronous completion — check for common case inline
                if (_lengthHolderBytes[0] == 0x0D && _lengthHolderBytes[1] == 0x0A)
                    return default; // No trailers — zero overhead

                return ParseTrailersSlowPathAsync(ct);
            }

            return AwaitReadThenCheckTrailersAsync(readTask, ct);
        }

        private async ValueTask AwaitReadThenCheckTrailersAsync(ValueTask<bool> readTask, CancellationToken ct)
        {
            await readTask.ConfigureAwait(false);

            if (_lengthHolderBytes[0] == 0x0D && _lengthHolderBytes[1] == 0x0A)
                return;

            await ParseTrailersSlowPathAsync(ct).ConfigureAwait(false);
        }

        private async ValueTask ParseTrailersSlowPathAsync(CancellationToken ct)
        {
            // Rare path: trailers present. Seed line builder with the 2 bytes already read.
            var trailerList = new List<HeaderField>();
            var lineBuilder = new StringBuilder();

            for (var i = 0; i < 2; i++)
            {
                if (_lengthHolderBytes[i] != 0x0A && _lengthHolderBytes[i] != 0x0D)
                    lineBuilder.Append((char)_lengthHolderBytes[i]);
            }

            while (true)
            {
                if (!await _innerStream.ReadExactAsync(_singleByte, ct).ConfigureAwait(false))
                    break;

                var b = _singleByte[0];

                if (b == 0x0D) // CR
                {
                    await _innerStream.ReadExactAsync(_singleByte, ct).ConfigureAwait(false);

                    if (lineBuilder.Length == 0)
                        break; // Empty line = end of trailers

                    ParseAndAddTrailerLine(lineBuilder, trailerList);
                }
                else if (b != 0x0A)
                {
                    lineBuilder.Append((char)b);
                }
            }

            if (trailerList.Count > 0)
                Trailers = trailerList;
        }

        /// <summary>
        ///     Sync version of trailer parsing. Same fast-path optimization.
        /// </summary>
        private void ParseTrailers()
        {
            // Fast path: read 2 bytes. If \r\n → no trailers.
            _innerStream.ReadExact(new Span<byte>(_lengthHolderBytes, 0, 2));

            if (_lengthHolderBytes[0] == 0x0D && _lengthHolderBytes[1] == 0x0A)
                return;

            var trailerList = new List<HeaderField>();
            var lineBuilder = new StringBuilder();
            Span<byte> single = _singleByte;

            for (var i = 0; i < 2; i++)
            {
                if (_lengthHolderBytes[i] != 0x0A && _lengthHolderBytes[i] != 0x0D)
                    lineBuilder.Append((char)_lengthHolderBytes[i]);
            }

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

                    ParseAndAddTrailerLine(lineBuilder, trailerList);
                }
                else if (b != 0x0A)
                {
                    lineBuilder.Append((char)b);
                }
            }

            if (trailerList.Count > 0)
                Trailers = trailerList;
        }

        private static void ParseAndAddTrailerLine(StringBuilder lineBuilder, List<HeaderField> trailerList)
        {
            var line = lineBuilder.ToString();
            lineBuilder.Clear();

            var colonIdx = line.IndexOf(':');

            if (colonIdx > 0)
            {
                trailerList.Add(new HeaderField(
                    line.Substring(0, colonIdx).Trim(),
                    line.Substring(colonIdx + 1).Trim()));
            }
        }
    }
}