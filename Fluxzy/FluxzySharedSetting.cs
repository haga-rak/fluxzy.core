// // Copyright 2022 - Haga Rakotoharivelo
// 

using System;

namespace Fluxzy
{
    public static class FluxzySharedSetting
    {
        public static bool IsRunningInDesktop => Environment.GetEnvironmentVariable("Desktop") == "true";
    }
}