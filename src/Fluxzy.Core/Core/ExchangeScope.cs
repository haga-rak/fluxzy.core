// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Collections.Generic;

namespace Fluxzy.Core
{
    public class ExchangeScope : IDisposable
    {
        private readonly List<IDisposable> _memoryPools = new();

        private bool _disposed;

        public Memory<char> RegisterForReturn(int length)
        {
            var memoryOwner = MemoryPool<char>.Shared.Rent(length);

            lock (this)
                _memoryPools.Add(memoryOwner);

            return memoryOwner.Memory.Slice(0, length);
        }
        
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            foreach (var memoryOwner in _memoryPools)
            {
                memoryOwner.Dispose();
            }
        }
    }
}
