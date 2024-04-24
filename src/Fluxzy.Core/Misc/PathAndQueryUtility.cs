// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Misc
{
    /// <summary>
    ///  This is a simple helper that aims to provide a way to extract the path and query from a URL.
    ///  We use this to have 0 allocations when we need to extract the path and query from a URL for plain HTTP.
    /// </summary>
    public static class PathAndQueryUtility
    {
        public static ReadOnlySpan<char> Parse(ReadOnlySpan<char> urlOrPath)
        {
            if (!urlOrPath.StartsWith("http"))
            {
                return urlOrPath;
            }

            int pathStart = urlOrPath.IndexOf("://");
            if (pathStart == -1)
            {
                return urlOrPath;
            }

            // Skip "://"
            pathStart += 3;

            // Find the start of the path by skipping to the first '/' after "://"

            var lastIndex = urlOrPath[pathStart..].IndexOf('/');

            if (lastIndex == -1)
            {
                return "/";
            }

            pathStart = lastIndex + pathStart;

            if (pathStart == -1)
            {
                return urlOrPath;
            }

            // From the start of the path to the end of the string
            ReadOnlySpan<char> pathAndQuery = urlOrPath[pathStart..];

            return pathAndQuery;
        }
    }
}
