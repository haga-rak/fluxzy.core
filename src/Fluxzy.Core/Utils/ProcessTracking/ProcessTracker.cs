// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Fluxzy.Utils.ProcessTracking
{
    /// <summary>
    /// Provides process tracking functionality using platform-specific APIs.
    /// Supports Windows, Linux, and macOS.
    /// </summary>
    public sealed class ProcessTracker : IProcessTracker
    {
        /// <summary>
        /// Default instance of the process tracker.
        /// </summary>
        public static readonly ProcessTracker Instance = new();

        private readonly ConcurrentDictionary<int, CachedProcessInfo> _cache = new();
        private static readonly TimeSpan CacheValidityDuration = TimeSpan.FromSeconds(30);

        private sealed class CachedProcessInfo
        {
            public required ProcessInfo Info { get; init; }
            public required DateTime CachedAt { get; init; }
        }

        public ProcessInfo? GetProcessInfo(int localPort)
        {
            var now = DateTime.UtcNow;

            // Check cache first
            if (_cache.TryGetValue(localPort, out var cached))
            {
                if (now - cached.CachedAt < CacheValidityDuration)
                    return cached.Info;

                // Expired, remove from cache
                _cache.TryRemove(localPort, out _);
            }

            // Cache miss or expired - fetch fresh data
            var result = GetProcessInfoInternal(localPort);

            if (result != null)
            {
                _cache[localPort] = new CachedProcessInfo { Info = result, CachedAt = now };
            }

            return result;
        }

        private static ProcessInfo? GetProcessInfoInternal(int localPort)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return WindowsProcessHelper.GetProcessInfo(localPort);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return LinuxProcessHelper.GetProcessInfo(localPort);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return MacOsProcessHelper.GetProcessInfo(localPort);

            throw new PlatformNotSupportedException(
                "ProcessTracker is only supported on Windows, Linux, and macOS.");
        }
    }
}
