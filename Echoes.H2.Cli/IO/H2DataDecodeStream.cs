using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Echoes.H2.Cli.Helpers;

namespace Echoes.H2.Cli.IO
{
    /// <summary>
    /// Remove h2 frames and produces raw data
    /// </summary>
    internal class H2DataDecodeStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly H2Frame _frame;
        private  bool ? _padded;
        private int _length; 

        public H2DataDecodeStream(Stream innerStream, H2Frame frame)
        {
            _innerStream = innerStream;
            _frame = frame;
            _padded = (frame.Flags & 0x8) != 0;
            _length = frame.BodyLength; 
        }

        public override void Flush()
        {
            throw new InvalidOperationException("not supported");
        }

        private int _currentOffset = 0;
        private int _padValue = 0;
        private bool _eof = false;

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_currentOffset >= _length)
                return 0;

            if (_padded != null)
            {
                if (_padded.Value)
                {
                    byte [] paddedBuffer = new byte[1];
                    if ((await _innerStream.ReadAsync(paddedBuffer, 0, paddedBuffer.Length, cancellationToken).ConfigureAwait(false)) != 1)
                    {
                        throw new EndOfStreamException("Unable to read from the stream");
                    }
                    _padValue = paddedBuffer[0];
                }

                _padded = null;
            }

            var remainReadable = count;

            if (remainReadable > (_length - _currentOffset))
            {
                remainReadable = _length - _currentOffset;
            }

            var actualReaden = await _innerStream.ReadAsync(buffer, offset, remainReadable, cancellationToken).ConfigureAwait(false);
            _currentOffset += actualReaden;

            if (_currentOffset == _length && _padValue > 0)
            {
                // Reading the pad 

                byte [] paddedData = new byte[_padValue];
                await _innerStream.ReadExactAsync(paddedData, 0, paddedData.Length, cancellationToken).ConfigureAwait(false);
            }

            return actualReaden;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_currentOffset >= _length)
                return 0;

            if (_padded != null)
            {
                if (_padded.Value)
                {
                    Span<byte> paddedBuffer = stackalloc byte[1];
                    if (_innerStream.Read(paddedBuffer) <= 0)
                    {
                        throw new EndOfStreamException("Unable to read from the stream");
                    };
                    _padValue = paddedBuffer[0];
                }
                
                _padded = null;
            }

            var remainReadable = count;

            if (remainReadable > (_length - _currentOffset))
            {
                remainReadable = _length - _currentOffset;
            }

            var actualReaden = _innerStream.Read(buffer, offset, remainReadable);
            _currentOffset += actualReaden;

            if (_currentOffset == _length && _padValue > 0)
            {
                // Reading the pad 
                
                Span<byte> data = stackalloc byte[_padValue];
                _innerStream.ReadExact(data); 
            }
            
            return actualReaden; 
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException("not supported"); 
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException("not supported");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("not supported");
        }

        public override bool CanRead => !_eof;

        public override bool CanSeek { get; } = false;

        public override bool CanWrite { get; } = true;

        public override long Length => _length;

        public override long Position
        {
            get
            {
                return _currentOffset; 
            }
            set
            {
                throw new InvalidOperationException("not supported");
            }
        }
    }
}