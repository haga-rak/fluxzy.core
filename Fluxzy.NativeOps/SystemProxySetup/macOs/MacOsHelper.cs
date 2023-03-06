// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using Fluxzy.Misc;

namespace Fluxzy.NativeOps.SystemProxySetup.macOs
{
    internal class MacOsHelper
    {
        // Adding root certificate on macos s
        // sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain r.cer 
        
        public static IEnumerable<Interface> GetEnabledInterfaces()
        {
            var runResult = ProcessUtils.QuickRun("networksetup", "-listnetworkserviceorder");

            if (runResult.ExitCode != 0 || runResult.StandardOutputMessage == null)
                throw new InvalidOperationException("Failed to get interfaces");

            var commandResponse = runResult.StandardOutputMessage;

            return  ParseInterfaces(commandResponse);
        }

        public static IEnumerable<Interface> ParseInterfaces(string commandResponse)
        {
            var lines = commandResponse
                .Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (var index = 0; index < lines.Length - 1; index++)
            {
                var line = lines[index];
                var nextLine = lines[index + 1];

                var iface = Interface.BuildFrom(new[] { line, nextLine});

                if (iface == null)
                    continue; 

                // parsing happens 

                index += 1; // skip next line because it 's already parsed

                yield return iface;
            }
        }


        public static void TrySetProxySettings(IEnumerable<Interface> interfaces)
        {
            foreach (var iFace in interfaces) {
                var interfaceName = iFace.Name;

                var commandLineResult = ProcessUtils.QuickRun("networksetup", $"-getwebproxy \"{interfaceName}\"");

                if (commandLineResult.ExitCode != 0 || commandLineResult.StandardOutputMessage == null)
                    continue;

                var commandResponse = commandLineResult.StandardOutputMessage;

                var proxySetting = InterfaceProxySetting.Get(commandResponse);  

                if (proxySetting == null)
                    continue;

                iFace.ProxySetting = proxySetting;
            }
        }
    }
}
