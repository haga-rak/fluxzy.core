using System;
using System.Linq;
using System.Runtime.InteropServices;
using Fluxzy.Core.SystemProxySetup;
using Fluxzy.Core.SystemProxySetup.Linux;
using Fluxzy.Core.SystemProxySetup.macOs;
using Fluxzy.Core.SystemProxySetup.Win32;

namespace Fluxzy.Core
{
    public class SystemProxyRegistration : IDisposable
    {
        private readonly string _hostName;
        private readonly int _port;
        private readonly ISystemProxySetter _systemProxySetter;
        private readonly ProxySetting _oldSetting;

        public SystemProxyRegistration(IConsoleOutput consoleOutput, string hostName, int port, params string[] byPassHosts)
        {
            _hostName = GetConnectableHostname(hostName);
            _port = port;
            _systemProxySetter = SolveSetter();

            if (_systemProxySetter != null)
            {
                _oldSetting = _systemProxySetter.ReadSetting();
                _systemProxySetter
                    .ApplySetting(
                        new ProxySetting(_hostName, port, true, byPassHosts.Concat(new[] { "127.0.0.1" }).ToArray()));

                consoleOutput.WriteLineAsync($"System proxy registered on {_hostName}:{port}");
            }
        }

        private string GetConnectableHostname(string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName))
                return "127.0.0.1";

            if (hostName == "0.0.0.0")
                return "127.0.0.1";

            return hostName;
        }

        public void Dispose()
        {
            if (_oldSetting.BoundHost == _hostName && _oldSetting.ListenPort == _port)
            {
                _oldSetting.Enabled = false; // Il s'agit d'un setting précédent mal libéré
            }

            _oldSetting.Enabled = false;
            _systemProxySetter?.ApplySetting(_oldSetting);
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


            return null;
        }
    }

}