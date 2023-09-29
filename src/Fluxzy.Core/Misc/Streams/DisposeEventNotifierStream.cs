// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Misc.Streams
{
    public delegate Task DisposeFunc(object sender, StreamDisposeEventArgs args);

    public class DisposeEventNotifierStream : Stream
    {
        public Stream InnerStream { get; }

        private int _totalRead;

        private bool _fromAsyncDispose = false;

        public bool Faulted { get; private set; }

        public DisposeEventNotifierStream(Stream innerStream)
        {
            InnerStream = innerStream;
        }

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
                //return 0; // JUST RETURN EOF when fail
            }
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new())
        {
            return await InnerStream.ReadAsync(buffer, cancellationToken);
        }

        public override async Task<int> ReadAsync(
            byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
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

        public override async ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new())
        {
            await InnerStream.WriteAsync(buffer, cancellationToken);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);
        }

        public override void Close()
        {
            Faulted = true; 

            if (!_fromAsyncDispose)
            {
                DisposeAsync();
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

            await InnerStream.DisposeAsync();

            if (OnStreamDisposed != null)
                await OnStreamDisposed(this, new StreamDisposeEventArgs());

            
        }
    }
}
