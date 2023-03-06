// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Clients.H2.Encoder.Utils
{
    public static class SpanCharExtensions
    {
        public static int GetHashCodeArray(this Span<char> obj)
        {
            if (obj.IsEmpty)
                return 0;

            unchecked {
                const int p = 16777619;

                var hash = (int) 2166136261;

                for (var i = 0; i < obj.Length; i++) {
                    hash = (hash ^ obj[i]) * p;
                }

                return hash;
            }
        }
    }
}
