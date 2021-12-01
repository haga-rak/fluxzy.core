using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2.IO
{
    internal class H2DataEncodeStream : Stream
    {
        private readonly Stream _innerStream;

        public H2DataEncodeStream(Stream innerStream, int length)
        {
            _innerStream = innerStream;
        }

        public override void Flush()
        {
            _innerStream.Flush(); 
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _innerStream.FlushAsync(cancellationToken); 
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("not supported");
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
        }

        public override bool CanRead { get; } = false;

        public override bool CanSeek { get; } = false; 
            
        public override bool CanWrite { get; }
        public override long Length { get; }
        public override long Position { get; set; }
    }
}