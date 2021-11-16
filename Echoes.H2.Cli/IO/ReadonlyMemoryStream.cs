using System;
using System.IO;

namespace Echoes.H2.Cli.IO
{
    internal class ReadonlyMemoryStream : Stream
    {
        private readonly Memory<byte> _memory;
        private int _currentOffset = 0; 

        public ReadonlyMemoryStream(Memory<byte> memory)
        {
            _memory = memory;
        }

        public override void Flush()
        {
        }

        public override int Read(Span<byte> buffer)
        {
            if (_currentOffset >= _memory.Length)
                return 0;

            var remaining = _memory.Length - _currentOffset;

            var copyable = buffer.Length > remaining ? remaining : buffer.Length;

            _memory.Span.Slice(_currentOffset).CopyTo(buffer);

            _currentOffset += copyable;

            return copyable;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_currentOffset >= _memory.Length)
                return 0;

            var remaining = _memory.Length - _currentOffset;

            var copyable = count > remaining ? remaining : count;

            _memory
                .Span.Slice(_currentOffset)
                .CopyTo(new Span<byte>(buffer, offset, copyable));

            _currentOffset += copyable;

            return copyable;

        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:

                    var finalOffset = offset;
                    if (finalOffset < 0 || finalOffset >= _memory.Length)
                        throw new IndexOutOfRangeException();
                    _currentOffset = (int)finalOffset;
                    return _currentOffset; 
                case SeekOrigin.Current:
                    finalOffset = _currentOffset + offset; 
                    if (finalOffset < 0 || finalOffset >= _memory.Length)
                        throw new IndexOutOfRangeException();
                    _currentOffset = (int) finalOffset;
                    return _currentOffset;
                case SeekOrigin.End:
                    finalOffset = _memory.Length - offset;

                    if (finalOffset < 0 || finalOffset >= _memory.Length)
                        throw new IndexOutOfRangeException();

                    _currentOffset = (int)finalOffset;

                    return _currentOffset;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
            }
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead { get; } = true;

        public override bool CanSeek { get; } = true;

        public override bool CanWrite { get; } = false;

        public override long Length => _memory.Length;

        public override long Position
        {
            get
            {
                return _currentOffset;
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}