using System;
using System.Collections.Generic;

namespace Echoes.H2.Encoder.Utils
{
    public class TableEntryComparer : IEqualityComparer<HeaderField> 
    {
        public static TableEntryComparer Default { get; } = new TableEntryComparer(); 

        public bool Equals(HeaderField x, HeaderField y)
        {
            // Header name is case insensitive
            return x.Name.Span.Equals(y.Name.Span, StringComparison.OrdinalIgnoreCase) &&
                   x.Value.Span.Equals(y.Value.Span, StringComparison.InvariantCulture);
        }

        public int GetHashCode(HeaderField obj)
        {
            unchecked
            {
                Span<char> buffer1 = stackalloc char[obj.Name.Span.Length];
                Span<char> buffer2 = stackalloc char[obj.Value.Span.Length];

                obj.Name.Span.ToLowerInvariant(buffer1);
                obj.Value.Span.ToLowerInvariant(buffer2);

                return (buffer1.GetHashCodeArray() * 397) ^ buffer2.GetHashCodeArray();
            }
        }
    }
}