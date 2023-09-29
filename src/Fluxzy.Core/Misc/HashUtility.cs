// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Misc
{
    public static class HashUtility
    {
        /// <summary>
        ///     A stable 64bits hash code based on Knut hash
        /// </summary>
        /// <param name="read"></param>
        /// <returns></returns>
        public static ulong GetLongHash(ReadOnlySpan<char> read)
        {
            var hashedValue = 3074457345618258791ul;

            for (var i = 0; i < read.Length; i++)
            {
                hashedValue += read[i];
                hashedValue *= 3074457345618258799ul;
            }

            return hashedValue;
        }

        public static ulong GetLongHash(string read)
        {
            return GetLongHash(read.AsSpan());
        }
    }
}
