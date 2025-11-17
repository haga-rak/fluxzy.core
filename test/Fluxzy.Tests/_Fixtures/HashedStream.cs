// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Tests._Fixtures
{
    /// <summary>
    ///     Read an inner stream and automatically produces hash
    /// </summary>
    internal class HashedStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly MemoryStream _outStream = new(new byte[64]);
        private readonly HashAlgorithm _transform;

        public HashedStream(Stream innerStream, bool useSha1 = false)
        {
            _transform = !useSha1 ? SHA256.Create() : SHA1.Create();
            _innerStream = innerStream;
        }

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanSeek => _innerStream.CanSeek;

        public override bool CanWrite => _innerStream.CanWrite;

        public override long Length => _innerStream.Length;

        public override long Position {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        public string Hash => Convert.ToBase64String(Compute() ?? Array.Empty<byte>());

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var res = _innerStream.Read(buffer, offset, count);

            _transform.TransformBlock(buffer, offset, res, null, 0);

            return res;
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

        public override async Task<int> ReadAsync(
            byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var res = await _innerStream.ReadAsync(buffer, offset, count, cancellationToken);

            _transform.TransformBlock(buffer, offset, res, null, 0);

            return res;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) {
                _innerStream.Dispose();
                _transform.Dispose();
                _outStream.Dispose();
            }

            base.Dispose(disposing);
        }

        public byte[]? Compute()
        {
            _transform.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            var array = _transform.Hash;

            return array;
        }
    }
}
