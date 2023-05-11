// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy
{
    public static class FluxzySharedSetting
    {
        public static bool IsRunningInDesktop => Environment.GetEnvironmentVariable("Desktop") == "true";

        public static int RequestProcessingBuffer { get; set; } = 1024 * 16; 
    }
}
