// Copyright © 2022 Haga RAKOTOHARIVELO

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
        private int _bufferLength;

        public SearchStream(Stream innerStream, ReadOnlyMemory<byte> searchPattern)
        {
            if (searchPattern.IsEmpty)
                throw new ArgumentException("cannot be empty", nameof(searchPattern));

            _innerStream = innerStream;
            _searchPattern = searchPattern;
            _rawBuffer = ArrayPool<byte>.Shared.Rent(searchPattern.Length * 2 + 2);
            _bufferOffset = 0L;
            _bufferLength = 0; 
        }

        public StreamSearchResult? Result { get; private set;  } = null;

        private StreamSearchResult? AddNewSequence(ReadOnlyMemory<byte> data)
        {
            Span<byte> fixedBuffer = _rawBuffer;

            var remainingSpace = fixedBuffer.Length - _bufferLength;

            if (remainingSpace < data.Length)
            {
                // Copy remaining space 

                // Copy data to buffer
                data.Span.Slice(0, remainingSpace).CopyTo(fixedBuffer.Slice(_bufferLength));
                
                data = data.Slice(remainingSpace);
                _bufferLength += remainingSpace;

                // Check for match 

                var matchingIndex =
                    fixedBuffer.Slice(0, _bufferLength)
                    .IndexOf(_searchPattern.Span);

                if (matchingIndex >= 0)
                {
                    return new StreamSearchResult(_bufferOffset + matchingIndex);
                }

                var offsetDivision = fixedBuffer.Length / 2 + 1;
                var shiftedLength = fixedBuffer.Length - offsetDivision;

                // Copy shifted length to offset 0 
                fixedBuffer.Slice(offsetDivision, shiftedLength)
                    .CopyTo(fixedBuffer);

                // Update bufferOffset 

                _bufferOffset += (fixedBuffer.Length - shiftedLength);

                // update buffer length 
                _bufferLength = shiftedLength;

                // Recursive call
                return AddNewSequence(data); // TODO : update whole block to iterative 
                
            }
            else
            {
                data.Span.CopyTo(fixedBuffer.Slice(_bufferLength));
                _bufferLength += data.Length;

                var matchingIndex =
                    fixedBuffer.Slice(0, _bufferLength)
                               .IndexOf(_searchPattern.Span);

                if (matchingIndex >= 0)
                {
                    return new StreamSearchResult(_bufferOffset + matchingIndex);
                }
            }

            return null; 
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
                Result = AddNewSequence(buffer.AsMemory(offset, read));
            }
            else
            {
                Result ??= StreamSearchResult.NotFound;
            }

            return read; 
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var read = await _innerStream.ReadAsync(buffer, cancellationToken);

            if (read > 0)
            {
                Result = AddNewSequence(buffer.Slice(0, read));
            }
            else
            {
                Result ??= StreamSearchResult.NotFound;
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
            _innerStream.Dispose();
            base.Dispose(disposing);
        }
    }   

    public class StreamSearchResult
    {
        public StreamSearchResult(long offsetFound)
        {
            OffsetFound = offsetFound;
        }

        public long OffsetFound { get; }

        public static StreamSearchResult NotFound => new StreamSearchResult(-1);


    }
}
