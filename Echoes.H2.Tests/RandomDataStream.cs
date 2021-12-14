// Copyright � 2021 Haga Rakotoharivelo

using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Echoes.H2.Tests
{
    public class RandomDataStream : Stream
    {
        private readonly int _length;
        private int _actualReaden;
        private readonly MemoryStream _memoryStream = new(); 

        private readonly Random _random;
        private readonly CryptoStream _cryptoStream;
        private readonly SHA1 _crypto;

        public RandomDataStream(int seed, int length)
        {
            _length = length;
            _random = new Random(seed);

            _crypto = SHA1.Create();

            _cryptoStream = new CryptoStream(_memoryStream, _crypto, CryptoStreamMode.Write); 
        }

        public string?  Hash { get; private set; }

        public override void Flush()
        {
        }

        public override int Read(byte [] buffer, int offset, int count)
        {
            var remains = _length - _actualReaden;

            var currentRead = Math.Min(count, remains);

            var current = new Span<byte>(buffer, offset, currentRead); 

            _random.NextBytes(current);
            _cryptoStream.Write(current);

            _actualReaden += currentRead;

            if (currentRead == 0)
                return 0;

            if (_actualReaden == _length)
            {
                _cryptoStream.FlushFinalBlock();
                Hash = MockedBinaryUtilities.GetStringSha256Hash(_memoryStream.ToArray());
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

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override ValueTask DisposeAsync()
        {
            _crypto.Dispose();
            return _cryptoStream.DisposeAsync(); ;


        }
    }
}