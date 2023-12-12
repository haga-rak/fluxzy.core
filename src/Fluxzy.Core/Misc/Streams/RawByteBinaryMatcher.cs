// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Misc.Streams
{
    public class RawByteBinaryMatcher : IBinaryMatcher
    {
        public int FindIndex(ReadOnlySpan<byte> buffer, ReadOnlySpan<byte> searchText)
        {
            return buffer.IndexOf(searchText);
        }
    }
}
