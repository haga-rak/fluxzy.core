using System;
using System.Runtime.InteropServices;
using Fluxzy.Core.Proxy;
using Fluxzy.NativeOps.SystemProxySetup.Linux;
using Fluxzy.NativeOps.SystemProxySetup.macOs;
using Fluxzy.NativeOps.SystemProxySetup.Win;

namespace Fluxzy.NativeOps
{
    public class NativeProxySetterManager : ISystemProxySetterManager
    {
        public ISystemProxySetter Get()
        {
             if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                 return new WindowsSystemProxySetter();

             if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                 return new LinuxProxySetter();

             if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                 return new MacOsProxySetter();

             throw new NotSupportedException("This platform is not supported");
        }
    }
}
