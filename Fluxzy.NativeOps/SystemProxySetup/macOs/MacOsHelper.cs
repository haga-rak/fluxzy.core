// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using Fluxzy.Misc;

namespace Fluxzy.NativeOps.SystemProxySetup.macOs
{
    internal class MacOsHelper
    {
        public static IEnumerable<Interface> GetInterfaces()
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
    }
}
