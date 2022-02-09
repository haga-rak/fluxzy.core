// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using Echoes.H2.Helpers;

namespace Echoes.H2.IO
{
    public class ChunckedReadStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly bool _closeOnDone;
        private bool _canRead; 

        public ChunckedReadStream(Stream innerStream, bool closeOnDone)
        {
            _innerStream = innerStream;
            _closeOnDone = closeOnDone;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        private long _nextChunkSize = -1;

        private char[] _lengthHolderChar = new char[64]; 
        private byte[] _lengthHolderBytes = new byte[64]; 
        
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

        public override bool CanRead => _canRead; 

        public override bool CanSeek { get; } = false; 

        public override bool CanWrite { get; } = false;

        public override long Length => throw new InvalidOperationException(); 

        public override long Position { get => throw new InvalidOperationException(); set => throw new InvalidOperationException();  }
    }
}