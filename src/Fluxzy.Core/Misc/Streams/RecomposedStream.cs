// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Misc.Streams
{
    /// <summary>
    ///     Aims to build an I/O stream from a read and a write stream
    /// </summary>
    internal class RecomposedStream : Stream
    {
        private readonly Stream _readStream;
        private readonly Stream _writeStream;

        public RecomposedStream(Stream readStream, Stream writeStream)
        {
            _readStream = readStream;
            _writeStream = writeStream;
        }

        public override bool CanRead => _readStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => _writeStream.CanWrite;

        public override long Length => throw new NotSupportedException();

        public override long Position { get; set; }

        public override void Flush()
        {
            _readStream.Flush();
            _writeStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.WhenAll(_readStream.FlushAsync(cancellationToken), _writeStream.FlushAsync(cancellationToken));
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _readStream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) {
                return Task.FromResult(0);
            }

            return _readStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new())
        {
            return await _readStream.ReadAsync(buffer, cancellationToken);
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
            _writeStream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested || !_writeStream.CanWrite) {
                return Task.CompletedTask;
            }

            return _writeStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override async ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
        {
            if (cancellationToken.IsCancellationRequested || !_writeStream.CanWrite) {
                return;
            }

            await _writeStream.WriteAsync(buffer, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            try {
                _readStream.Dispose();
            }
            catch {
            }

            try {
                _writeStream.Dispose();
            }
            catch {
            }
        }
    }
}
