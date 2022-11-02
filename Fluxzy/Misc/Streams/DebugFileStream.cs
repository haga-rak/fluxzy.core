using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Misc.Streams
{
    /// <summary>
    /// Used to dump a stream to file. 
    /// </summary>
    public class DebugFileStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly FileStream? _fileStreamIn;
        private readonly FileStream? _fileStreamOut;

        public DebugFileStream(string pathPrefix, Stream innerStream, bool? readOnly = null)
        {
            _innerStream = innerStream;

            if (readOnly == null)
            {
                if (_innerStream.CanRead)
                    _fileStreamIn = File.Create(pathPrefix + ".in.txt");

                if (_innerStream.CanWrite)
                    _fileStreamOut = File.Create(pathPrefix + ".out.txt");
            }
            else
            {
                if (readOnly.Value)
                {
                    _fileStreamIn = File.Create(pathPrefix + ".in.txt");
                }
                else
                {
                    _fileStreamOut = File.Create(pathPrefix + ".out.txt");
                }
            }
        }

        public override void Flush()
        {

        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var res = _innerStream.Read(buffer, offset, count);

            if (_fileStreamIn != null)
            {
                _fileStreamIn.Write(buffer, offset, res);
                _fileStreamIn.Flush();

            }


            return res;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            var res = await _innerStream.ReadAsync(buffer, cancellationToken);

            if (_fileStreamIn != null)
            {
                await _fileStreamIn.WriteAsync(buffer.Slice(0, res), cancellationToken);
                await _fileStreamIn.FlushAsync(cancellationToken);
            }

            return res;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, SeekOrigin.Begin);
        }

        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_fileStreamOut != null)
                _fileStreamOut.Write(buffer, offset, count);
            _innerStream.Write(buffer, offset, count);

            if (_fileStreamOut != null)
                _fileStreamOut.Flush();
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            if (_fileStreamOut != null)
                await _fileStreamOut.WriteAsync(buffer, cancellationToken);
            await _innerStream.WriteAsync(buffer, cancellationToken);

            if (_fileStreamOut != null)
                await _fileStreamOut.FlushAsync(cancellationToken);
        }

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;
        public override long Position
        {
            get => _innerStream.Position; set => _innerStream.Position = value;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fileStreamIn?.Dispose();
                _fileStreamOut?.Dispose();
            }

            base.Dispose(disposing);
        }

    }
}