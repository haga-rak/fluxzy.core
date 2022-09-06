using System;

namespace Fluxzy.Core.SystemProxySetup.Linux
{
    internal class LinuxProxySetter : ISystemProxySetter
    {
        public void ApplySetting(SystemProxySetting value)
        {
            throw new PlatformNotSupportedException();
        }

        public SystemProxySetting ReadSetting()
        {
            throw new PlatformNotSupportedException();
        }
    }
}