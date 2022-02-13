using System.IO;

namespace Echoes.Core.Utils
{
    public class SpyiedStream : Stream
    {
        static SpyiedStream()
        {
            //new DirectoryInfo(".").EnumerateFiles("*txt").Select(f =>
            //{
            //    f.Delete(); return 0;
            //}).ToList();
        }

        private readonly Stream _original;
        private readonly string _fileName;

        public SpyiedStream(Stream original, string hostname)
        {
            _original = original;
            _fileName = string.Intern($"{hostname}.txt");
        }

        public override void Flush()
        {
            _original.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _original.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _original.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var result =  _original.Read(buffer, offset, count);

            if (result > 0)
            {
                lock (_fileName)
                {
                    using (var fileStream = new FileStream(_fileName, FileMode.Append))
                    {
                        fileStream.Write(buffer, offset, result);
                    }
                }
            }

            return result; 
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (_fileName)
            {

                using (var fileStream = new FileStream(_fileName, FileMode.Append))
                {
                    fileStream.Write(buffer, offset, count);
                }
            }

            _original.Write(buffer, offset, count);
        }

        public override bool CanRead  => _original.CanRead; 

        public override bool CanSeek => _original.CanSeek;

        public override bool CanWrite => _original.CanWrite;

        public override long Length => _original.Length;

        public override long Position
        {
            get
            {
                return _original.Position;
            }
            set
            {
                _original.Position = value;
            }
        }
    }
}
