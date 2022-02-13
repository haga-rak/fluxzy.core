using System;

namespace Echoes.H2.Encoder.Utils
{
    public static class AsciiStringHelper
    {
        public static int GetHashCodeArray(this Span<char> obj)
        {
            if (obj.IsEmpty)
            {
                return 0;
            }

            unchecked
            {
                const int p = 16777619;

                int hash = (int) 2166136261;

                for (int i = 0; i < obj.Length; i++)
                    hash = (hash ^ obj[i]) * p;

                return hash;
            }
        }
    }
}