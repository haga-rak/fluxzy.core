// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Core.Proxy;
using Fluxzy.Misc;
using Fluxzy.Utils.NativeOps.SystemProxySetup.Win;

namespace Fluxzy.Utils.NativeOps.SystemProxySetup.macOs
{
    internal class MacOsProxySetter : ISystemProxySetter
    {
        private const string NativeSettingKey = "nativeSetting";

        private static readonly HashSet<string> PriorityDevices = new(
            new[] { "Wi-Fi", "Ethernet"}, StringComparer.OrdinalIgnoreCase);

        public async Task<SystemProxySetting> ReadSetting()
        {
            var interfaces = await MacOsHelper.GetEnabledInterfaces();

            await MacOsHelper.PopulateProxySettings(interfaces);

            var enabledProxySetting = interfaces
                                      .Where(s => s.ProxySetting != null)
                                      .OrderByDescending(t => PriorityDevices.Contains(t.HardwarePort))
                                      .FirstOrDefault(s => s.ProxySetting!.Enabled)?.ProxySetting;

            var proxyEnabled = enabledProxySetting != null;
            var proxyHost = enabledProxySetting?.Server ?? ProxyConstants.NoProxyWord;
            var proxyPort = enabledProxySetting?.Port ?? -1;
            var byPassDomains = enabledProxySetting?.ByPassDomains ?? Array.Empty<string>();

            // The per-interface snapshot is what UnRegister replays to restore the exact prior state.
            return new SystemProxySetting(proxyHost, proxyPort, byPassDomains) {
                Enabled = proxyEnabled,
                PrivateValues = { [NativeSettingKey] = interfaces }
            };
        }

        public async Task ApplySetting(SystemProxySetting value)
        {
            var throwOnFail = false;

#if DEBUG
            throwOnFail = true;
#endif

            // Restoring a previously read setting: replay each interface's captured state verbatim.
            if (value.PrivateValues.TryGetValue(NativeSettingKey, out var native)
                && native is IEnumerable<NetworkInterface> snapshot) {
                await Task.WhenAll(snapshot.Select(s => RestoreInterface(s, throwOnFail)));

                return;
            }

            // Fresh setting: apply the same proxy to every active service.
            var serviceNames = (await MacOsHelper.GetEnabledInterfaces()).Select(s => s.ServiceName);

            var appliedHost = value.BoundHost == ProxyConstants.NoProxyWord ? string.Empty : value.BoundHost;
            var appliedPort = value.ListenPort <= 0 ? 0 : value.ListenPort;

            await Task.WhenAll(serviceNames.Select(
                s => ApplyToService(s, appliedHost, appliedPort, value.ByPassHosts, value.Enabled, throwOnFail)));
        }

        private static async Task RestoreInterface(NetworkInterface iface, bool throwOnFail)
        {
            if (iface.ProxySetting is not { } proxy) {
                // Prior state unknown: just turn the proxy off rather than guessing a host.
                await SetState(iface.ServiceName, false, throwOnFail);

                return;
            }

            await ApplyToService(iface.ServiceName, proxy.Server, proxy.Port, proxy.ByPassDomains, proxy.Enabled,
                throwOnFail);
        }

        private static async Task ApplyToService(
            string serviceName, string host, int port, IEnumerable<string> byPassHosts, bool enabled, bool throwOnFail)
        {
            await ProcessUtils.QuickRunAsync("networksetup",
                new[] { "-setwebproxy", serviceName, host, port.ToString() }, throwOnFail);

            await ProcessUtils.QuickRunAsync("networksetup",
                new[] { "-setsecurewebproxy", serviceName, host, port.ToString() }, throwOnFail);

            await SetByPassDomains(serviceName, byPassHosts, throwOnFail);

            await SetState(serviceName, enabled, throwOnFail);
        }

        private static async Task SetByPassDomains(string serviceName, IEnumerable<string> byPassHosts, bool throwOnFail)
        {
            var domains = byPassHosts.Where(h => !string.IsNullOrWhiteSpace(h)).ToList();

            var args = new List<string> { "-setproxybypassdomains", serviceName };

            // networksetup clears the list only via the "Empty" keyword; an empty argument
            // would otherwise be stored as a single blank bypass entry.
            args.AddRange(domains.Count == 0 ? new[] { "Empty" } : domains);

            await ProcessUtils.QuickRunAsync("networksetup", args, throwOnFail);
        }

        private static async Task SetState(string serviceName, bool enabled, bool throwOnFail)
        {
            var onOff = enabled ? "on" : "off";

            await ProcessUtils.QuickRunAsync("networksetup",
                new[] { "-setwebproxystate", serviceName, onOff }, throwOnFail);

            await ProcessUtils.QuickRunAsync("networksetup",
                new[] { "-setsecurewebproxystate", serviceName, onOff }, throwOnFail);
        }
    }
}
