// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Clients.H2.Encoder.Utils
{
    public class ArrayPoolMemoryProvider<T>
    {
        public static ArrayPoolMemoryProvider<T> Default { get; } = new();

        public Memory<T> Allocate(ReadOnlySpan<T> span)
        {
            var res = new T[span.Length];
            span.CopyTo(res);

            return res;
        }
    }
}
