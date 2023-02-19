using System;
using System.Runtime.InteropServices;
using Fluxzy.Core.Proxy;
using Fluxzy.NativeOp.SystemProxySetup.Linux;
using Fluxzy.NativeOp.SystemProxySetup.macOs;
using Fluxzy.NativeOp.SystemProxySetup.Win32;

namespace Fluxzy.NativeOp
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