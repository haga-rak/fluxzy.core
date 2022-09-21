// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Misc.Streams
{
    public class ChunkedTransferWriteStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly byte[] _chunkLengthBuffer = new byte[64];
        private static readonly byte[] ChunkTerminator = { (byte)'0', (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };
        private static readonly byte[] LineTerminator = { (byte)'\r', (byte)'\n' };

        private bool _eof;

        public ChunkedTransferWriteStream(Stream innerStream)
        {
            _innerStream = innerStream;
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
            int cs = Encoding.ASCII.GetBytes($"{count:X}\r\n", _chunkLengthBuffer);
            _innerStream.Write(_chunkLengthBuffer, 0, cs);
            _innerStream.Write(buffer, offset, count);
            _innerStream.Write(LineTerminator);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            int cs = Encoding.ASCII.GetBytes($"{buffer.Length:X}\r\n", _chunkLengthBuffer);
            await _innerStream.WriteAsync(new ReadOnlyMemory<byte>(_chunkLengthBuffer, 0, cs), cancellationToken);
            await _innerStream.WriteAsync(buffer, cancellationToken);
            await _innerStream.WriteAsync(new ReadOnlyMemory<byte>(LineTerminator), cancellationToken);
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
        }

        public async ValueTask WriteEof()
        {
            if (!_eof)
            {
                _eof = true;
                await _innerStream.WriteAsync(new ReadOnlyMemory<byte>(ChunkTerminator));
            }

        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => !_eof;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
    }
}