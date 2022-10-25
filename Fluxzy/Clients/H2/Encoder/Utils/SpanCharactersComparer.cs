using System;
using System.Buffers;
using System.Collections.Generic;

namespace Fluxzy.Clients.H2.Encoder.Utils
{
    public class SpanCharactersIgnoreCaseComparer : IEqualityComparer<ReadOnlyMemory<char>>
    {
        public static SpanCharactersIgnoreCaseComparer Default { get; } = new SpanCharactersIgnoreCaseComparer(); 

        public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
        {
            if (x.Length != y.Length)
                return false; 
            
            return x.Span.Equals(y.Span, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(ReadOnlyMemory<char> obj)
        {

            char[]? heapBuffer = null;
            try
            {
                Span<char> buffer1 = obj.Span.Length < 1024 ? stackalloc char[obj.Span.Length] :
                     heapBuffer = ArrayPool<char>.Shared.Rent(obj.Span.Length); 

                obj.Span.ToLowerInvariant(buffer1);

                return (buffer1.GetHashCodeArray());
            }
            finally
            {
                if (heapBuffer != null)
                {
                    ArrayPool<char>.Shared.Return(heapBuffer);
                }
            }
        }
    }
}