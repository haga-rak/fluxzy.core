// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Collections.Concurrent;

namespace Fluxzy.Core
{
    public class ExchangeScope : IDisposable
    {
        private readonly ConcurrentBag<char[]> _charArrayPool = new();

        public Memory<char> RegisterForReturn(int length)
        {
            var array = ArrayPool<char>.Shared.Rent(length);
            _charArrayPool.Add(array);
            return new Memory<char>(array, 0, length);
        }

        public void RegisterForReturn(char[] array)
        {
            _charArrayPool.Add(array);
        }

        public void Dispose()
        {
            foreach (var array in _charArrayPool)
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }
    }
}
