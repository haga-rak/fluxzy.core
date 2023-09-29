// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Runtime.InteropServices;
using Fluxzy.Core.Proxy;
using Fluxzy.Utils.NativeOps.SystemProxySetup.Linux;
using Fluxzy.Utils.NativeOps.SystemProxySetup.macOs;
using Fluxzy.Utils.NativeOps.SystemProxySetup.Win;

namespace Fluxzy.Utils.NativeOps.SystemProxySetup
{
    public class NativeProxySetterManager : ISystemProxySetterManager
    {
        public ISystemProxySetter Get()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new WindowsSystemProxySetter();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return new LinuxProxySetter();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return new MacOsProxySetter();

            // TODO : return a "do-nothing" implementation instead of throwing
            throw new NotSupportedException("This platform is not supported");
        }
    }
}
