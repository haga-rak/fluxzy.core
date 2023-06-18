// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Misc;

namespace Fluxzy.NativeOps.SystemProxySetup.macOs
{
    internal class MacOsHelper
    {
        // Adding root certificate on macos s

        public static async Task<IEnumerable<NetworkInterface>> GetEnabledInterfaces()
        {
            var runResult = await ProcessUtils.QuickRunAsync("networksetup", "-listnetworkserviceorder");

            if (runResult.ExitCode != 0 || runResult.StandardOutputMessage == null)
                throw new InvalidOperationException("Failed to get interfaces");

            var commandResponse = runResult.StandardOutputMessage;

            return ParseInterfaces(commandResponse).Where(r => r.HardwarePort.Equals("Ethernet", StringComparison.OrdinalIgnoreCase)
            || r.HardwarePort.Equals("Wi-Fi", StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<NetworkInterface> ParseInterfaces(string commandResponse)
        {
            var lines = commandResponse
                .Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (var index = 0; index < lines.Length - 1; index++) {
                var line = lines[index];
                var nextLine = lines[index + 1];

                var iface = NetworkInterface.BuildFrom(new[] { line, nextLine });

                if (iface == null)
                    continue;

                // parsing happens 

                index += 1; // skip next line because it 's already parsed

                yield return iface;
            }
        }

        public static async Task<Dictionary<string, NetworkInterfaceProxySetting?>> ReadProxySettings(IEnumerable<string> interfaceNames)
        {
            var result = new Dictionary<string, NetworkInterfaceProxySetting?>();

            foreach (var interfaceName in interfaceNames) {

                var getwebProxyResult = await ProcessUtils.QuickRunAsync("networksetup", $"-getsecurewebproxy \"{interfaceName}\"");

                if (getwebProxyResult.ExitCode != 0 || getwebProxyResult.StandardOutputMessage == null)
                    continue;

                var commandResponse = getwebProxyResult.StandardOutputMessage;

                var proxySetting = NetworkInterfaceProxySetting.ParseFromCommandLineResult(commandResponse);

                result[interfaceName] = proxySetting;

                if (proxySetting != null) {
                    // Proxy settigns is available we try to set bypass domains

                    var byPassDomainsRunResult = await ProcessUtils.QuickRunAsync("networksetup", $"-getproxybypassdomains \"{interfaceName}\"");

                    if (byPassDomainsRunResult.ExitCode != 0 || byPassDomainsRunResult.StandardOutputMessage == null)
                        continue;

                    var byPassDomains = byPassDomainsRunResult.StandardOutputMessage.Split(new[] { "\r", "\n" },
                        StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();

                    proxySetting.ByPassDomains = byPassDomains;
                }
            }

            return result;
        }
    }
}
