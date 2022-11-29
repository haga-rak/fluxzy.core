// Copyright © 2022 Haga RAKOTOHARIVELO

using System;
using System.Buffers;
using System.IO;

namespace Fluxzy.Misc
{
    /// <summary>
    /// Search byte sequence in a stream for relatively small input.
    /// Note : A buffer with twice the size of searchPattern is allocated. 
    /// </summary>
    public class SearchStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly ReadOnlyMemory<byte> _searchPattern;
        private readonly byte[] _rawBuffer;

        /// <summary>
        /// The offset of current buffer relative to stream 
        /// </summary>
        private long _bufferOffset;

        /// <summary>
        /// The used length inside the buffer 
        /// </summary>
        private readonly int _bufferLength;

        public SearchStream(Stream innerStream, ReadOnlyMemory<byte> searchPattern)
        {
            _innerStream = innerStream;
            _searchPattern = searchPattern;
            _rawBuffer = ArrayPool<byte>.Shared.Rent(searchPattern.Length * 2);
            _bufferOffset = 0L;
            _bufferLength = 0; 
        }

        public StreamSearchResult? Result { get; } = null;

        private void AddNewSequence(ReadOnlyMemory<byte> data)
        {
            var remainingSpace = _searchPattern.Length - _bufferLength;

            while (remainingSpace < data.Length)
            {




            }
        }


        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _innerStream.Read(buffer, offset, count);

            if (read > 0)
            {

            }

            return read; 
            
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            // We let Seekable to be true to let stream user request for length.
            // it's a common practice to define Seekable = true when Length is available 

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

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanSeek => _innerStream.CanSeek;

        public override bool CanWrite => false; 

        public override long Length => _innerStream.Length;

        public override long Position
        {
            get => _innerStream.Position;

            set => _innerStream.Position = value;
        }

        protected override void Dispose(bool disposing)
        {
            ArrayPool<byte>.Shared.Return(_rawBuffer);
            base.Dispose(disposing);
        }
    }   

    public class StreamSearchResult
    {
        public long OffsetFound { get; }
    }
}
