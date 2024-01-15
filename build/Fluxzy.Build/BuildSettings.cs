// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Build
{
    internal static class BuildSettings
    {
        public static bool SkipSigning { get; } = string.Equals(Environment.GetEnvironmentVariable("NO_SIGN"), "1");
        
        public static int ConcurrentSignCount { get; } =
            int.Parse(Environment.GetEnvironmentVariable("CONCURRENT_SIGN")?.Trim() ?? "6");
    }
}
