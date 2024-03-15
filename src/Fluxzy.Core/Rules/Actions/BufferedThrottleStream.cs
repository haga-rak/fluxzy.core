// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Rules.Actions
{
    internal class BufferedThrottleStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly AverageThrottler _averageThrottler;

        public BufferedThrottleStream(Stream innerStream, AverageThrottler averageThrottler)
        {
            _innerStream = innerStream;
            _averageThrottler = averageThrottler;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Supports only async"); 
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Seek is not supported"); 
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Seek is not supported");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Supports only async");
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            var read = await _innerStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

            var readSize = _averageThrottler.ComputeThrottleDelay(read);

            if (readSize > 0) {
                await Task.Delay(readSize, cancellationToken).ConfigureAwait(false);
            }

            return read; 
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Supports only async");
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotSupportedException("Supports only async");
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException("Seek is not supported");

        public override long Position {
            get
            {
                throw new NotSupportedException("Seek is not supported");
            }
            set
            {
                throw new NotSupportedException("Seek is not supported");
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _innerStream.Dispose();
        }
    }
}
