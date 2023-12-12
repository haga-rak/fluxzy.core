// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Misc.Streams
{
    public interface IBinaryMatcher
    {
        int FindIndex(ReadOnlySpan<byte> buffer, ReadOnlySpan<byte> searchText); 
    }
}
