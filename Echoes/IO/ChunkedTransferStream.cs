// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Echoes.H2;
using Echoes.Helpers;

namespace Echoes.IO
{
    public class ChunkedTransferStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly bool _closeOnDone;

        private readonly char[] _lengthHolderChar = new char[64];
        private readonly byte[] _lengthHolderBytes = new byte[64];

        public ChunkedTransferStream(Stream innerStream, bool closeOnDone)
        {
            _innerStream = innerStream;
            _closeOnDone = closeOnDone;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        private long _nextChunkSize = -1;

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            // Read the length of the next block 

            if (_nextChunkSize == -1)
            {
                Memory<byte> textBufferBytes = _lengthHolderBytes;
                Memory<char> textBufferChars = _lengthHolderChar;

                int current = 0;
                int textCount = 0;

                while ((current = await _innerStream.ReadAsync(textBufferBytes, cancellationToken)) > 0 && current != 0XD)
                {
                    textBufferBytes.Span[textCount++] = (byte)current;
                }

                if (current != 0XD)
                {
                    throw new ExchangeException(
                        $"EOF was reached on chunked stream before reading a valid length block.");
                }

                System.Text.Encoding.ASCII.GetChars(textBufferBytes.Span.Slice(0, textCount), textBufferChars.Span);

                if (!long.TryParse(textBufferChars.Slice(0, textCount).Span,
                        NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                        out var chunkSize) || chunkSize < 0)
                {
                    throw new ExchangeException(
                        $"Received chunk size is invalid : {textBufferChars.Slice(0, textCount).ToString()}.");
                }

                if (chunkSize == 0)
                {
                    if (!_closeOnDone)
                        await _innerStream.ReadExactAsync(textBufferBytes.Slice(0, 3), cancellationToken);

                    return 0;
                }

                // Skip the next CRLF 
                await _innerStream.ReadExactAsync(textBufferBytes.Slice(0, 3), cancellationToken);

                _nextChunkSize = chunkSize;
            }

            var nextBlockToRead = (int)Math.Min(_nextChunkSize + 2, buffer.Length);

            var read = await _innerStream.ReadAsync(buffer.Slice(0, nextBlockToRead), cancellationToken);

            var chunkDataComplete = read == _nextChunkSize + 2;

            if (chunkDataComplete)
                read -= 2;

            if (read <= 0)
            {
                throw new ExchangeException(
                    $"EOF was reached before receiving {_nextChunkSize} bytes of chunked data.");
            }

            _nextChunkSize = chunkDataComplete ? -1 : _nextChunkSize - read;

            return read;
        }

        public override int Read(byte [] buffer, int offset, int count)
        {
            // Read the length of the next block 

            if (_nextChunkSize == -1)
            {
                Span<byte> textBufferBytes = _lengthHolderBytes;
                Span<char> textBufferChars = _lengthHolderChar;

                int current = 0;
                int textCount = 0;

                while ((current = _innerStream.Read(textBufferBytes)) > 0 && current != 0XD)
                {
                    textBufferBytes[textCount++] = (byte) current; 
                }

                if (current != 0XD)
                {
                    throw new ExchangeException(
                        $"EOF was reached on chunked stream before reading a valid length block.");
                }

                System.Text.Encoding.ASCII.GetChars(textBufferBytes.Slice(0, textCount), textBufferChars);

                if (!long.TryParse(textBufferChars.Slice(0, textCount), 
                        NumberStyles.HexNumber, CultureInfo.InvariantCulture,  
                        out var chunkSize) || chunkSize < 0)
                {
                    throw new ExchangeException(
                        $"Received chunk size is invalid : {textBufferChars.Slice(0, textCount).ToString()}.");
                }

                if (chunkSize == 0)
                {
                    if (!_closeOnDone)
                        _innerStream.ReadExact(textBufferBytes.Slice(0, 3));

                    return 0; 
                }

                _innerStream.ReadExact(textBufferBytes.Slice(0, 3));
                _nextChunkSize = chunkSize; 
            }

            var nextBlockToRead = (int) Math.Min(_nextChunkSize + 2, count);

            var read = _innerStream.Read(buffer, offset, nextBlockToRead);

            var chunkDataComplete = read == _nextChunkSize + 2;
            
            if (chunkDataComplete)
                read -= 2;

            if (read <= 0)
            {
                throw new ExchangeException(
                    $"EOF was reached before receiving {_nextChunkSize} bytes of chunked data.");
            }
            
            _nextChunkSize = chunkDataComplete ? -1 : _nextChunkSize - read;
            
            return read; 
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => true; 

        public override bool CanSeek { get; } = false; 

        public override bool CanWrite { get; } = false;

        public override long Length => throw new InvalidOperationException(); 

        public override long Position { get => throw new InvalidOperationException(); set => throw new InvalidOperationException();  }
    }
}