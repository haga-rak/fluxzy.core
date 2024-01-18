// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;

namespace Fluxzy.Benchmarks
{
    [MemoryDiagnoser(false)]
    public class Md5VsSha256
    {
        private const int N = 10000;
        private readonly byte[] _data;

        private readonly SHA256 _sha256 = SHA256.Create();
        private readonly MD5 _md5 = MD5.Create();

        public Md5VsSha256()
        {
            _data = new byte[N];
            new Random(42).NextBytes(_data);
        }

        [Benchmark]
        public byte[] Sha256()
        {
            return _sha256.ComputeHash(_data);
        }

        [Benchmark]
        public byte[] Md5()
        {
            return _md5.ComputeHash(_data);
        }
    }
}
