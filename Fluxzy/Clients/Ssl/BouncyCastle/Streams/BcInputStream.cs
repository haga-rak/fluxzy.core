using System;
using System.IO;
using System.Net.Sockets;

namespace Fluxzy.Clients.Ssl.BouncyCastle.Streams
{


    
    internal class BcInputStream : Stream
    {
        /// <summary>
        /// The underlined networkstream 
        /// </summary>
        private readonly NetworkStream _networkStream;

        public BcInputStream(NetworkStream networkStream)
        {
            _networkStream = networkStream;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _networkStream.Read(buffer, offset, count); 
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead { get; }
        public override bool CanSeek { get; }
        public override bool CanWrite { get; }
        public override long Length { get; }
        public override long Position { get; set; }
    }
}
