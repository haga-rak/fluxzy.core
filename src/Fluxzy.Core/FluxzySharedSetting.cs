// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy
{
    /// <summary>
    /// Contains unique settings per instance
    /// </summary>
    public static class FluxzySharedSetting
    {
        static FluxzySharedSetting()
        {
            var rawValue = Environment.GetEnvironmentVariable("OverallMaxConcurrentConnections");

            if (rawValue != null && int.TryParse(rawValue, out var value) && value > 0)
                OverallMaxConcurrentConnections = value;
        }

        public static bool IsRunningInDesktop => Environment.GetEnvironmentVariable("Desktop") == "true";

        public static int RequestProcessingBuffer { get; set; } = 1024 * 4; 

        public static int OverallMaxConcurrentConnections { get;  } = 102400;

        public static bool Use528 { get; set; } = true; 
    }
}
