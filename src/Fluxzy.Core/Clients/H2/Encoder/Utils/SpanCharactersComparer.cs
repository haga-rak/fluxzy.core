// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;

namespace Fluxzy.Clients.H2.Encoder.Utils
{
    public class SpanCharactersIgnoreCaseComparer : IEqualityComparer<ReadOnlyMemory<char>>
    {
        public static SpanCharactersIgnoreCaseComparer Default { get; } = new();

        public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
        {
            if (x.Length != y.Length)
                return false;

            return x.Span.Equals(y.Span, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(ReadOnlyMemory<char> obj)
        {
            return string.GetHashCode(obj.Span, StringComparison.OrdinalIgnoreCase);
        }
    }
}
