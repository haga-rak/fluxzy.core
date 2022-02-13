// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Buffers;
using System.Collections.Generic;
using Echoes.H2.Encoder.Utils.Interfaces;

namespace Echoes.H2.Encoder.Utils
{
    public class ArrayPoolMemoryProvider<T> : IMemoryProvider<T>
    {
        private readonly ArrayPool<T> _arrayBuffer = ArrayPool<T>.Create(1024 * 32,4096);
        private readonly List<T[]> _allocated = new List<T[]>(); 

        public T [] Allocate(int size)
        {
            var newArray = _arrayBuffer.Rent(size);
            _allocated.Add(newArray);

            return newArray; 
        }

        public Memory<T> Allocate(ReadOnlySpan<T> span)
        {
            var memoryResult = new Memory<T>(Allocate(span.Length)) ;
            span.CopyTo(memoryResult.Span);
            return memoryResult.Slice(0, span.Length); 
        }

        public void Dispose()
        {
            foreach (var array in _allocated)
            {
                _arrayBuffer.Return(array); 
            }
        }
    }
}