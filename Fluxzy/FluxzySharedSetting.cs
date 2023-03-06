// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy
{
    public static class FluxzySharedSetting
    {
        public static bool IsRunningInDesktop => Environment.GetEnvironmentVariable("Desktop") == "true";
    }
}
