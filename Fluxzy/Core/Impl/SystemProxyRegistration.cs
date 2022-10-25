using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using Fluxzy.Core.SystemProxySetup;
using Fluxzy.Core.SystemProxySetup.Linux;
using Fluxzy.Core.SystemProxySetup.macOs;
using Fluxzy.Core.SystemProxySetup.Win32;

namespace Fluxzy.Core
{
    /// <summary>
    /// System proxy management is static because related to OS management. 
    /// </summary>
    public static class SystemProxyRegistration
    {
        private static readonly ISystemProxySetter _systemProxySetter;
        private static SystemProxySetting? _oldSetting;
        private static SystemProxySetting? _currentSetting;
        private static bool _registerDone = false;

        static SystemProxyRegistration()
        {
            _systemProxySetter = SolveSetter();
        }

        public static SystemProxySetting? Register(IEnumerable<IPEndPoint> endPoints, FluxzySetting fluxzySetting)
        {
            return Register(endPoints.OrderByDescending(t => Equals(t.Address, IPAddress.Loopback)
             || t.Address.Equals(IPAddress.IPv6Loopback)).First(), fluxzySetting.ByPassHost.ToArray()); 
        }


        public static SystemProxySetting Register(IPEndPoint endPoint, params string[] byPassHosts)
        {
            var existingSetting = GetSystemProxySetting();

            if (!existingSetting.Equals(_oldSetting))
            {
                _oldSetting = existingSetting;
            }

            if (!_registerDone)
            {
                _registerDone = true;
                ProxyUnregisterOnAppdomainExit();
            }

            var connectableHostName = GetConnectableIpAddr(endPoint.Address);

            _currentSetting = new SystemProxySetting(
                connectableHostName.ToString(),
                endPoint.Port, 
                byPassHosts.Concat(new[] { "127.0.0.1" })
                    .ToArray());

            _systemProxySetter.ApplySetting(_currentSetting);

            return _currentSetting; 
        }

        public static SystemProxySetting GetSystemProxySetting()
        {
            var existingSetting = _systemProxySetter.ReadSetting();
            return existingSetting;
        }

        public static void UnRegister()
        {
            if (_oldSetting != null)
            {
                _systemProxySetter.ApplySetting(_oldSetting);
                _oldSetting = null; 
                return; 
            }

            if (_currentSetting != null)
            {
                _currentSetting.Enabled = false; 
                _systemProxySetter.ApplySetting(_currentSetting);
                _currentSetting = null; 
                return; 
            }

            var existingSetting = GetSystemProxySetting();

            if (existingSetting.Enabled)
            {
                existingSetting.Enabled = false;
                _systemProxySetter.ApplySetting(existingSetting);
            }

        }

        private static IPAddress GetConnectableIpAddr(IPAddress address)
        {
            if (address == null)
                return IPAddress.Loopback;

            if (address.Equals(IPAddress.Any)) {
                return IPAddress.Loopback; 
            }

            if (address.Equals(IPAddress.IPv6Any)) {
                return IPAddress.IPv6Loopback; 
            }
            
            return address;
        }
        
        private static ISystemProxySetter SolveSetter()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WindowsSystemProxySetter();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new LinuxProxySetter();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new MacOsProxySetter();
            }
            
            throw new NotImplementedException(); 
        }

        private static void ProxyUnregisterOnAppdomainExit()
        {
            AppDomain.CurrentDomain.ProcessExit += delegate(object sender, EventArgs args)
            {
                UnRegister();
            };
        }
    }

}