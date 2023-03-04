// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fluxzy.NativeOps.SystemProxySetup.macOs
{
    internal class Interface
    {
        public Interface(int index, string name, string deviceName)
        {
            Index = index;
            Name = name;
            DeviceName = deviceName;
        }

        public string Name { get;  } 

        public string DeviceName { get;  } 

        public int Index { get;  }
        
        public bool Up { get; set; }
        public InterfaceProxySetting? ProxySetting { get; set; }

        public static Interface?  BuildFrom(string[] lines)
        {
            if (lines.Length != 2)
                return null;

            var regexInterfaceName = @"^\((\d+)\) (.*)$"; 
            var regexDeviceName = @"Device: ([a-zA-Z0-9_]+)\)$";

            var matchInterfaceName = Regex.Match(lines[0], regexInterfaceName);

            if (!matchInterfaceName.Success)
                return null;

            var interfaceName = matchInterfaceName.Groups[2].Value; 
            var deviceIndex = int.Parse(matchInterfaceName.Groups[1].Value);

            var matchDeviceName = Regex.Match(lines[1], regexDeviceName);

            if (!matchDeviceName.Success) 
                return null;

            var deviceName = matchDeviceName.Groups[1].Value;

            return new Interface(deviceIndex, interfaceName, deviceName);
        }
    }

    internal class InterfaceProxySetting
    {
        public InterfaceProxySetting(bool enabled, string server, int port)
        {
            Enabled = enabled;
            Server = server;
            Port = port;
        }

        public bool Enabled { get;  }

        public string Server { get; }

        public int Port { get;  }


        public static InterfaceProxySetting? Get(string commandLineResult)
        {
            var lines = commandLineResult.Split(new[] { "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries)
                                         .ToList();

            Dictionary<string, string> dictionaryOfValue;

            try {
                dictionaryOfValue = lines.Select(line => line.Split(':'))
                                         .ToDictionary(split => split[0], split => string.Join(":", split.Skip(1)));
            }
            catch (ArgumentException) {
                return null;
            }

            if (!dictionaryOfValue.TryGetValue("Enabled", out var enabledValue))
                return null;

            if (!dictionaryOfValue.TryGetValue("Server", out var serverValue))
                return null; 

            if (!dictionaryOfValue.TryGetValue("Port", out var portValue))
                return null;

            var enabled = string.Equals(enabledValue.Trim(), "Yes", StringComparison.OrdinalIgnoreCase);
            var server = serverValue.Trim(); 
            
            if (!int.TryParse(portValue.Trim(), out var port))
                return null;

            return new InterfaceProxySetting(enabled, server, port);

        }
    }
}
