// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Fluxzy.Tests._Fixtures
{
    public class RandomDataStream : Stream
    {
        private readonly SHA1 _crypto;
        private readonly CryptoStream _cryptoStream;
        private readonly int _length;
        private readonly MemoryStream _memoryStream = new();

        private readonly Random _random;
        private readonly bool _seekable;
        private int _actualReaden;

        private bool _disposed;

        public RandomDataStream(int seed, int length, bool seekable = false)
        {
            _length = length;
            _seekable = seekable;
            _random = new Random(seed);

            _crypto = SHA1.Create();

            _cryptoStream = new CryptoStream(_memoryStream, _crypto, CryptoStreamMode.Write);
        }

        public string? Hash { get; private set; }

        public string? HashBae { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => _seekable;

        public override bool CanWrite => false;

        public override long Length =>
            _seekable
                ? _length - _actualReaden
                : throw new NotSupportedException();

        public override long Position {
            get => _actualReaden;
            set
            {
                if (value == 0)
                    return;

                throw new NotSupportedException();
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var remains = _length - _actualReaden;

            var currentRead = Math.Min(count, remains);

            var current = new Span<byte>(buffer, offset, currentRead);

            _random.NextBytes(current);
            _cryptoStream.Write(current);

            _actualReaden += currentRead;

            if (currentRead == 0)
                return 0;

            if (_actualReaden == _length) {
                _cryptoStream.FlushFinalBlock();
                var array = _memoryStream.ToArray();

                var rawHash = _crypto.Hash ?? Array.Empty<byte>();

                var hash =
                    Convert.ToHexString(rawHash).Replace("-", string.Empty);

                Hash = hash;

                HashBae = Convert.ToBase64String(rawHash);
            }

            return currentRead;
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

        public override ValueTask DisposeAsync()
        {
            if (_disposed)
                return default;

            _disposed = true;

            _crypto.Dispose();

            return default;
        }
    }
}
