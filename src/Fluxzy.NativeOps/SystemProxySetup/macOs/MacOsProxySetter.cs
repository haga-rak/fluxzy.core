// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Core.Proxy;
using Fluxzy.Misc;
using Fluxzy.NativeOps.SystemProxySetup.Win;

namespace Fluxzy.NativeOps.SystemProxySetup.macOs
{
    internal class MacOsProxySetter : ISystemProxySetter
    {
        private static readonly HashSet<string> PriorityDevices = new(
            new[] { "Wi-Fi", "Ethernet"}, StringComparer.OrdinalIgnoreCase); 

        public async Task<SystemProxySetting> ReadSetting()
        {
            // list all interfaces 
            var interfaces = (await MacOsHelper.GetEnabledInterfaces()).ToList();

            // get proxy settings
            var proxySettings = await MacOsHelper.ReadProxySettings(interfaces.Select(s => s.Name));

            foreach (var proxySetting in proxySettings) {
                var iface = interfaces.FirstOrDefault(s => s.Name == proxySetting.Key);

                if (iface == null)
                    continue;

                iface.ProxySetting = proxySetting.Value;
            }
            
            var activeInterfaces = interfaces.Where(s => s.ProxySetting != null).ToList();

            var enabledProxySetting = activeInterfaces
                                      .OrderByDescending(t => PriorityDevices.Contains(t.HardwarePort))
                                      .FirstOrDefault(s => s.ProxySetting!.Enabled)?.ProxySetting;

            var proxyEnabled = enabledProxySetting != null;
            var proxyHost = enabledProxySetting?.Server ?? ProxyConstants.NoProxyWord; 
            var proxyPort = enabledProxySetting?.Port ?? -1;
            var byPassDomains = enabledProxySetting?.ByPassDomains ?? Array.Empty<string>();

            return new SystemProxySetting(proxyHost, proxyPort, byPassDomains) {
                Enabled = proxyEnabled,
                PrivateValues = { ["nativeSetting"] = activeInterfaces } 
            };
        }

        public async Task ApplySetting(SystemProxySetting value)
        {
            // networksetup -setwebproxy Ethernet 127.0.0.1 44344
            // networksetup -setproxybypassdomains domain1 domain2 

            var activeInterfaceNames = (await MacOsHelper.GetEnabledInterfaces())
                                                  .Select(s => s.Name).ToList();


            var throwOnFail = false;

#if DEBUG
            throwOnFail = false; 
#endif
            await Task.WhenAll(activeInterfaceNames.Select(s => PrepareInterface(value, s, throwOnFail))); 
        }

        private static async Task PrepareInterface(SystemProxySetting value, string interfaceName, bool throwOnFail)
        {
            var appliedHost = value.BoundHost == ProxyConstants.NoProxyWord ? "''" : value.BoundHost;
            var appliedPort = value.ListenPort <= 0 ? "''" : value.ListenPort.ToString();

            await ProcessUtils.QuickRunAsync("networksetup",
                $"-setwebproxy \"{interfaceName}\" \"{appliedHost}\" {appliedPort}", throwOnFail: throwOnFail);

            await ProcessUtils.QuickRunAsync("networksetup",
                $"-setsecurewebproxy \"{interfaceName}\" \"{appliedHost}\" {appliedPort}", throwOnFail: throwOnFail);

            if (value.ByPassHosts.Any()) {
                await ProcessUtils.QuickRunAsync("networksetup",
                    $"-setproxybypassdomains \"{interfaceName}\" {string.Join(" ", value.ByPassHosts.Select(s => $"\"{s}\""))}",
                    throwOnFail: throwOnFail);
            }
            else {
                await ProcessUtils.QuickRunAsync("networksetup", $"-setproxybypassdomains \"{interfaceName}\" ''",
                    throwOnFail: throwOnFail);
            }

            var onOff = value.Enabled ? "on" : "off";

            await ProcessUtils.QuickRunAsync("networksetup", $"-setwebproxystate \"{interfaceName}\" {onOff}",
                throwOnFail: throwOnFail);

            await ProcessUtils.QuickRunAsync("networksetup", $"-setsecurewebproxystate \"{interfaceName}\" {onOff}",
                throwOnFail: throwOnFail);
        }
    }
}
