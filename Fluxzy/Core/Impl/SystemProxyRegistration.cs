using System;
using System.Linq;
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

        public static void Register(FluxzySetting fluxzySetting)
        {
            var boundPoints = fluxzySetting.BoundPoints;

            if (!boundPoints.Any())
            {
                return; 
            }

            var firstBoundPoint = boundPoints.OrderByDescending(t => 
                            t.Address.Equals("127.0.0.1") || t.Address.Equals("localhost")).First();

            Register(firstBoundPoint.Address, firstBoundPoint.Port, fluxzySetting.ByPassHost.ToArray()); 
        }


        public static void Register(string hostName, int port, params string[] byPassHosts)
        {
            var existingSetting = GetSystemProxySetting();

            if (!existingSetting.Equals(_oldSetting))
            {
                _oldSetting = existingSetting;
            }

            if (!_registerDone)
            {
                ProxyUnregisterOnAppdomainExit();
                _registerDone = true; 
            }

            var connectableHostName = GetConnectableHostname(hostName);

            _currentSetting = new SystemProxySetting(connectableHostName,
                port, byPassHosts.Concat(new[] { "127.0.0.1" })
                    .ToArray());

            _systemProxySetter.ApplySetting(_currentSetting);
        }

        internal static SystemProxySetting GetSystemProxySetting()
        {
            var existingSetting = _systemProxySetter.ReadSetting();
            return existingSetting;
        }

        public static void UnRegister()
        {
            if (_oldSetting != null)
            {
                _systemProxySetter?.ApplySetting(_oldSetting);
                _oldSetting = null; 
                return; 
            }

            if (_currentSetting != null)
            {
                _currentSetting.Enabled = false; 
                _systemProxySetter?.ApplySetting(_currentSetting);
                _currentSetting = null; 
                return; 
            }
        }

        private static string GetConnectableHostname(string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName))
                return "127.0.0.1";

            if (hostName == "0.0.0.0")
                return "127.0.0.1";

            return hostName;
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