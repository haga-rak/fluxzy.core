// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Misc.Streams
{
    /// <summary>
    /// Low level specification used to find a specific byte sequence in a buffer
    /// </summary>
    public interface IBinaryMatcher
    {
        BinaryMatchResult FindIndex(ReadOnlySpan<byte> buffer, ReadOnlySpan<byte> searchText); 
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Index">The start position where the pattern is found</param>
    /// <param name="Length"></param>
    /// <param name="ShiftLength"></param>
    public record struct BinaryMatchResult(int Index, int Length, int ShiftLength);
}
