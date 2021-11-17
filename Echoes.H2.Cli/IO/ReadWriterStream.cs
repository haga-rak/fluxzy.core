using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Echoes.H2.Cli.IO
{
    internal class ReadWriterStream : Stream
    {
        private readonly Memory<byte> _buffer;
        private int _index = 0;
        private bool _end = false;
        private Channel<Memory<byte>> _upChannel = Channel.CreateUnbounded<Memory<byte>>(); 

        public ReadWriterStream(Memory<byte> buffer, long ? length)
        {
            _buffer = buffer;
            Length = length ?? -1; 

        }
        
        internal void AppendBuffer(Memory<byte> data, bool end)
        {
            _upChannel.Writer.WriteAsync(data)
        }

        public override void Flush()
        {
        }

        private Memory<byte> _remainder; 

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            


            return base.ReadAsync(buffer, cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException("Please consider using ReadAsync() "); 
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new System.NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new System.NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }

        public override bool CanRead { get; } = true; 
        public override bool CanSeek { get; } = false; 
        public override bool CanWrite { get; } = false;
        public override long Length { get; } = -1; 
        public override long Position { get; set; }
    }
    
}