// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Buffers;

namespace Echoes.Helpers
{
    internal class MutableMemoryOwner<T> : IMemoryOwner<T>
    {
        private readonly IMemoryOwner<T> _existing;

        public MutableMemoryOwner(IMemoryOwner<T> existing, int length)
        {
            _existing = existing;
            Memory = existing.Memory.Slice(0, length);
        }

        public void Dispose()
        {
            _existing.Dispose();
        }

        public Memory<T> Memory { get; set; }
    }

    internal static class MemoryPoolRentExtensions
    {
        public static MutableMemoryOwner<T> RendExact<T>(this MemoryPool<T> memoryPool, int bufferSize)
        {
            return new MutableMemoryOwner<T>(memoryPool.Rent(bufferSize), bufferSize);
        }
    }
}