using System;
using System.Collections.Generic;
using System.IO;

namespace Fluxzy.Clients.H2.Encoder.Utils
{
    public class AsyncArrayReadonlyStream : Stream
    {
        private readonly IEnumerable<Memory<byte>> _arrayStream;
        private readonly int _bufferSize;
        private bool _endOfStream;
        private int currentOffset; 

        public AsyncArrayReadonlyStream(IEnumerable<Memory<byte>> arrayStream, int bufferSize = 16 * 1024)
        {
            _arrayStream = arrayStream;
            _bufferSize = bufferSize;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte [] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException();
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }

        public override bool CanRead => !_endOfStream; 

        public override bool CanSeek { get; } = false; 

        public override bool CanWrite { get; } = false;

        public override long Length => throw new InvalidOperationException();

        public override long Position
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }
    }
}