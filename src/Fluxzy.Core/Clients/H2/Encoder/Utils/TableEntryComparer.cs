// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;

namespace Fluxzy.Clients.H2.Encoder.Utils
{
    public class TableEntryComparer : IEqualityComparer<HeaderField>
    {
        public static TableEntryComparer Default { get; } = new();

        public bool Equals(HeaderField x, HeaderField y)
        {
            // Header name is case insensitive, value is ordinal (ASCII)
            return x.Name.Span.Equals(y.Name.Span, StringComparison.OrdinalIgnoreCase) &&
                   x.Value.Span.SequenceEqual(y.Value.Span);
        }

        public int GetHashCode(HeaderField obj)
        {
            unchecked {
                var nameSpan = obj.Name.Span;
                var valueSpan = obj.Value.Span;

                // FNV-1a hash over lowercased name + raw value
                const uint fnvOffset = 2166136261;
                const uint fnvPrime = 16777619;

                var hash = fnvOffset;

                for (var i = 0; i < nameSpan.Length; i++) {
                    // ASCII-lowercase inline (headers are ASCII)
                    var c = nameSpan[i];
                    if ((uint)(c - 'A') <= ('Z' - 'A'))
                        c = (char)(c | 0x20);
                    hash = (hash ^ c) * fnvPrime;
                }

                for (var i = 0; i < valueSpan.Length; i++) {
                    hash = (hash ^ valueSpan[i]) * fnvPrime;
                }

                return (int)hash;
            }
        }
    }
}
