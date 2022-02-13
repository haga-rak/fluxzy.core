using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.Core.Utils
{

    public static class StreamNetStandar2Extensions
    {
        public static Task WriteAsyncNS2(this Stream stream, byte[] buffer)
        {
            return stream.WriteAsync(buffer, 0, buffer.Length); 
        }
    }

    public class NetworkStream2 : Stream
    {
        private readonly NetworkStream _innerStream;
        private readonly Socket _innerSocket;

        public NetworkStream2(NetworkStream innerStream, Socket innerSocket)
        {
            _innerStream = innerStream;
            _innerSocket = innerSocket;
        }
        
        public override void Flush()
        {

        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _innerStream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            return _innerStream.ReadAsync(buffer, offset, count, token); 
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin); 
        }

        public override void SetLength(long value)
        {
             _innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {

            _innerSocket.Send(buffer, offset, count, SocketFlags.None);
            return Task.CompletedTask;
        }

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanSeek => _innerStream.CanSeek;

        public override bool CanWrite => _innerStream.CanWrite;

        public override long Length => _innerStream.Length;

        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _innerSocket.Dispose();
                _innerStream.Dispose();
            }
        }
    }
    
}