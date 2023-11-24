// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Core
{
    public class PipedSubstitution : IStreamSubstitution
    {
        public ValueTask<Stream> Substitute(Stream stream)
        {
            var pipe = new Pipe(PipeOptions.Default);
            return new ValueTask<Stream>();
        }
    }

    public interface IStreamListener
    {
        ValueTask<int> OnNewBufferAsync(ReadOnlyMemory<byte> inBuffer, Memory<byte> writeBuffer, bool endOfStream);

        void OnEndOfStream(); 
    }

    public class PipedTransfer : IStreamListener
    {
        private readonly Pipe _pipe;
        private readonly Pipe _pipeOutStream;
        private readonly Stream _outStream;

        public PipedTransfer()
        {
            _pipe = new Pipe(PipeOptions.Default);
            _pipeOutStream = new Pipe(PipeOptions.Default);
            _outStream = _pipeOutStream.Reader.AsStream(); 
        }
        
        public async ValueTask<int> OnNewBufferAsync(ReadOnlyMemory<byte> inBuffer, Memory<byte> writeBuffer, bool endOfStream)
        {
            await _pipe.Writer.WriteAsync(inBuffer);

            var read = await _outStream.ReadAsync(writeBuffer);
            return read;
        }

        public void OnEndOfStream()
        {
            // Trigger a complete 
            _pipe.Writer.Complete(); 
        }

        public Stream OutStream => _outStream;
    }

    public class HookedStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly IStreamListener _listener;
        private readonly int _readBuffer;

        public HookedStream(Stream innerStream, IStreamListener listener, int readBuffer = 4096)
        {
            _innerStream = innerStream;
            _listener = listener;
            _readBuffer = readBuffer;
        }

        public override void Flush()
        {

        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Only async operation are handled by HookStream"); 
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            var readBuffer = ArrayPool<byte>.Shared.Rent(_readBuffer);

            try {
                var read = await _innerStream.ReadAsync(readBuffer, cancellationToken);

                var outLength = await _listener.OnNewBufferAsync(readBuffer),


            }
            finally {
                ArrayPool<byte>.Shared.Return(readBuffer);
            }



            try
            {
                    outBuffer, read == 0);

                outBuffer.AsMemory(0, outLength).CopyTo(buffer);

                return outLength;
            }
            finally {

                if (read == 0)
                {
                    _listener.OnEndOfStream();
                }
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return ReadAsync(buffer, offset, count, cancellationToken);
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

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false; 

        public override long Length => throw new NotSupportedException();

        public override long Position {
            get { throw new NotSupportedException(); } set { throw new NotSupportedException(); }
        }
    }
}
