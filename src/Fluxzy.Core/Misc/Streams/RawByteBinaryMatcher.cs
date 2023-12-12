// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Misc.Streams
{
    public class RawByteBinaryMatcher : IBinaryMatcher
    {
        public BinaryMatchResult FindIndex(ReadOnlySpan<byte> buffer, ReadOnlySpan<byte> searchText)
        {
            return new (buffer.IndexOf(searchText), searchText.Length, searchText.Length);
        }
    }
}
