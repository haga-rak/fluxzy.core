// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Buffers;
using System.Collections.Generic;
using Echoes.H2.Encoder.Utils.Interfaces;

namespace Echoes.H2.Encoder.Utils
{
    public class ArrayPoolMemoryProvider<T> 
    {
        public static ArrayPoolMemoryProvider<T> Default { get; } = new();
        
        public Memory<T> Allocate(ReadOnlySpan<T> span)
        {
            var res =  new T[span.Length];
            span.CopyTo(res);
            return res; 
        }
    }
}