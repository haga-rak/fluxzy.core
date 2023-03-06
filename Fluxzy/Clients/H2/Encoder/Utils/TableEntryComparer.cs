// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Collections.Generic;

namespace Fluxzy.Clients.H2.Encoder.Utils
{
    public class TableEntryComparer : IEqualityComparer<HeaderField>
    {
        public static TableEntryComparer Default { get; } = new();

        public bool Equals(HeaderField x, HeaderField y)
        {
            // Header name is case insensitive
            return x.Name.Span.Equals(y.Name.Span, StringComparison.OrdinalIgnoreCase) &&
                   x.Value.Span.Equals(y.Value.Span, StringComparison.InvariantCulture);
        }

        public int GetHashCode(HeaderField obj)
        {
            unchecked {
                char[]? heapBuffer = null;

                try {
                    Span<char> buffer1 = stackalloc char[obj.Name.Span.Length];

                    var buffer2 = obj.Value.Span.Length < 1024
                        ? stackalloc char[obj.Value.Span.Length]
                        : heapBuffer =
                            ArrayPool<char>.Shared.Rent(obj.Value.Span.Length);

                    buffer2 = buffer2.Slice(0, obj.Value.Span.Length);

                    obj.Value.Span.ToLowerInvariant(buffer2);

                    return (buffer1.GetHashCodeArray() * 397) ^ buffer2.GetHashCodeArray();
                }
                finally {
                    if (heapBuffer != null)
                        ArrayPool<char>.Shared.Return(heapBuffer);
                }
            }
        }
    }
}
