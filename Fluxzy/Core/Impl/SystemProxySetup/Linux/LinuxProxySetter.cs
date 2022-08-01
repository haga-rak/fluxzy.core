using System;

namespace Echoes.Core.SystemProxySetup.Linux
{
    internal class LinuxProxySetter : ISystemProxySetter
    {
        public void ApplySetting(ProxySetting value)
        {
            throw new PlatformNotSupportedException();
        }

        public ProxySetting ReadSetting()
        {
            throw new PlatformNotSupportedException();
        }
    }
}