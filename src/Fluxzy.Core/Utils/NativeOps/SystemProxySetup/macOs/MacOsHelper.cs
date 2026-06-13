// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Fluxzy.Misc;

namespace Fluxzy.Utils.NativeOps.SystemProxySetup.macOs
{
    internal class MacOsHelper
    {
        public static async Task<IReadOnlyList<NetworkInterface>> GetEnabledInterfaces()
        {
            var runResult = await ProcessUtils.QuickRunAsync("networksetup", new[] { "-listnetworkserviceorder" });

            if (runResult.ExitCode != 0 || runResult.StandardOutputMessage == null)
                throw new InvalidOperationException("Failed to get interfaces");

            return ParseInterfaces(runResult.StandardOutputMessage);
        }

        public static IReadOnlyList<NetworkInterface> ParseInterfaces(string commandResponse)
        {
            var services = NetworkInterface.ParseNetworkServices(
                commandResponse.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries));

            var activeDevices = System.Net.NetworkInformation.NetworkInterface
                                       .GetAllNetworkInterfaces()
                                       .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                                       .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Unknown)
                                       .Where(n => n.OperationalStatus == OperationalStatus.Up)
                                       .Where(n => n.GetIPProperties().UnicastAddresses.Any())
                                       .Select(n => n.Name)
                                       .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return services.Where(s => activeDevices.Contains(s.DeviceName)).ToList();
        }

        /// <summary>
        /// Fills <see cref="NetworkInterface.ProxySetting"/> for each interface by querying
        /// <c>networksetup</c> by service name. Read failures leave the setting null.
        /// </summary>
        public static async Task PopulateProxySettings(IEnumerable<NetworkInterface> interfaces)
        {
            foreach (var iface in interfaces) {

                var proxyResult = await ProcessUtils.QuickRunAsync(
                    "networksetup", new[] { "-getsecurewebproxy", iface.ServiceName });

                if (proxyResult.ExitCode != 0 || proxyResult.StandardOutputMessage == null)
                    continue;

                var proxySetting = NetworkInterfaceProxySetting.ParseFromCommandLineResult(proxyResult.StandardOutputMessage);

                if (proxySetting == null)
                    continue;

                var byPassResult = await ProcessUtils.QuickRunAsync(
                    "networksetup", new[] { "-getproxybypassdomains", iface.ServiceName });

                if (byPassResult.ExitCode == 0 && byPassResult.StandardOutputMessage != null)
                    proxySetting.ByPassDomains = ParseByPassDomains(byPassResult.StandardOutputMessage);

                iface.ProxySetting = proxySetting;
            }
        }

        public static string[] ParseByPassDomains(string commandResponse)
        {
            // When the list is empty, networksetup prints "There aren't any bypass domains set on <service>."
            return commandResponse.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(l => l.Trim())
                                  .Where(l => l.Length > 0)
                                  .Where(l => !l.Contains("aren't any bypass", StringComparison.OrdinalIgnoreCase))
                                  .Distinct()
                                  .ToArray();
        }
    }
}
