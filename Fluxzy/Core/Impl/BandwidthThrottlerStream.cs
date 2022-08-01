using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Core
{
    /// <summary>
    /// This stream limit bandwidth while writing to it
    /// </summary>
    internal class BandwidthThrottlerStream : Stream
    {
        private readonly BandwidthThrottler _throwThrottler;

        public BandwidthThrottlerStream(BandwidthThrottler throwThrottler)
        {
            _throwThrottler = throwThrottler;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
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

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_throwThrottler == null)
                return;

            await _throwThrottler.CreateDelay(count).ConfigureAwait(false);
        }

        public override bool CanRead { get; } = false;

        public override bool CanSeek { get; } = false;

        public override bool CanWrite { get; } = true; 

        public override long Length { get; }

        public override long Position { get; set; }
    }

    internal class NoThrottleStream : Stream
    {
        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
        }

        public override bool CanRead { get; } = false;

        public override bool CanSeek { get; } = false;

        public override bool CanWrite { get; } = true; 

        public override long Length { get; } 

        public override long Position { get; set; }
    }
}
