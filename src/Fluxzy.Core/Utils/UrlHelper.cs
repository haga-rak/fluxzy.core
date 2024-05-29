// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Utils
{
    internal static class UrlHelper
    {
        public static bool IsAbsoluteHttpUrl(ReadOnlySpan<char> rawUrl)
        {
            return rawUrl.StartsWith("http://".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
                   rawUrl.StartsWith("https://".AsSpan(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
