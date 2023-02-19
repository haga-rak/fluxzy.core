using System;
using System.Collections.Generic;
using System.Linq;
using Fluxzy.Core.Proxy;
using Fluxzy.Misc;

namespace Fluxzy.NativeOp.SystemProxySetup.macOs
{
    internal class MacOsProxySetter : ISystemProxySetter
    {
        public void ApplySetting(SystemProxySetting value)
        {
            throw new NotImplementedException();
        }

        public SystemProxySetting ReadSetting()
        {
            var testedIfaces = new string[] { "Ethernet", "Wi-fi" };
            SystemProxySetting? pendingResult = null;

            List<string>? byPassDomains = new List<string>(); 

            var bypass = 
                ProcessUtils.RunAndExpectZero("networksetup",
                    $"-getproxybypassdomains {testedIfaces.First()}");

            if (bypass != null)
            {
                byPassDomains.AddRange(bypass
                    .Split(new[] { "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries));
            }

            foreach (var iFace in testedIfaces)
            {
                var input =
                    ProcessUtils.RunAndExpectZero("networksetup", $"-getwebproxy {iFace}");

                if (input == null || !TryReadProxySetting(input, out var address, out var port, out var enabled))
                    continue;

                if (pendingResult == null)
                {
                  //  pendingResult  = new SystemProxySetting()
                }
                
            }


            return null; 
        }

        private static bool TryReadProxySetting(
            string input,
            out string address, out int port, out bool enabled)
        {
            address = string.Empty;
            port = default;
            enabled = default; 

            var lineTab = input.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(s =>
                                   s.Split(new[] { ":" }, 2, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(v => v.Trim()).ToArray())
                               .Where(s => s.Length > 1)
                               .ToDictionary(s => s[0], s => s[1]);

            if (!lineTab.TryGetValue("Server", out address))
                return false; 

            if (!lineTab.TryGetValue("Port", out var portString)
                  || !int.TryParse(portString, out port))
                return false; 

            if (!lineTab.TryGetValue("Enabled", out var enabledString))
                return false;

            enabled = enabledString.Equals("Yes", StringComparison.OrdinalIgnoreCase); 

            return true; 
        }
    }
}
