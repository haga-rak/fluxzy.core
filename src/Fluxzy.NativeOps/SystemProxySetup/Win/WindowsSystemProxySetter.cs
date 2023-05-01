// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Runtime.Versioning;
using Fluxzy.Core.Proxy;

namespace Fluxzy.NativeOps.SystemProxySetup.Win
{
    [SupportedOSPlatform("windows")]
    internal class WindowsSystemProxySetter : ISystemProxySetter
    {
        public void ApplySetting(SystemProxySetting value)
        {
            WindowsProxyHelper.SetProxySetting(value);
        }

        public SystemProxySetting ReadSetting()
        {
            return WindowsProxyHelper.GetSetting();
        }
    }
}
