// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;

namespace Fluxzy.Misc.Streams
{
    public class ProcessStream : Stream
    {
        private bool _canRead;

        public ProcessStream(Stream innerStream)
        {

        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override bool CanRead => _canRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false; 

        public override long Length => throw new NotSupportedException();

        public override long Position {
            get
            {
                throw new NotSupportedException(); 
            }
            set
            {
                throw new NotSupportedException();
            }
        }
    }


}
