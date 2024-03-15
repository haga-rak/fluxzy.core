// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Misc.Streams
{
    public delegate ValueTask DisposeFunc(object sender, StreamDisposeEventArgs args);

    public class DisposeEventNotifierStream : Stream
    {
        private bool _fromAsyncDispose;

        private int _totalRead;

        public DisposeEventNotifierStream(Stream innerStream)
        {
            InnerStream = innerStream;
        }

        public Stream InnerStream { get; }

        public bool Faulted { get; private set; }

        public override bool CanRead => InnerStream.CanRead;

        public override bool CanSeek => InnerStream.CanSeek;

        public override bool CanWrite => InnerStream.CanWrite;

        public override long Length => InnerStream.Length;

        public override long Position {
            get => InnerStream.Position;
            set => InnerStream.Position = value;
        }

        public event DisposeFunc? OnStreamDisposed;

        public override void Flush()
        {
            InnerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            try {
                var res = InnerStream.Read(buffer, offset, count);

                _totalRead += res;

                return res;
            }
            catch {
                Faulted = true;

                throw;
            }
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new())
        {
            return await InnerStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<int> ReadAsync(
            byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).ConfigureAwait(false);
        }

        public override int Read(Span<byte> buffer)
        {
            return InnerStream.Read(buffer);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return InnerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            InnerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            InnerStream.Write(buffer, offset, count);
        }

        public override ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
        {
            return InnerStream.WriteAsync(buffer, cancellationToken);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).ConfigureAwait(false);
        }

        public override void Close()
        {
            Faulted = true;

            if (!_fromAsyncDispose) {
                _ = DisposeAsync();
            }
        }

        protected override void Dispose(bool disposing)
        {
            Faulted = true;

            //if (!_fromAsyncDispose) {
            //    DisposeAsync();
            //}

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            _fromAsyncDispose = true;
            Faulted = true;

            await InnerStream.DisposeAsync().ConfigureAwait(false);

            if (OnStreamDisposed != null) {
                await OnStreamDisposed(this, new StreamDisposeEventArgs()).ConfigureAwait(false);
            }
        }
    }
}
