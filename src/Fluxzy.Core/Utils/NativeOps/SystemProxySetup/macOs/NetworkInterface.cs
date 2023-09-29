// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fluxzy.Utils.NativeOps.SystemProxySetup.macOs
{
    internal class NetworkInterface
    {
        public NetworkInterface(string name, string deviceName, string hardwarePort)
        {
            Name = name;
            DeviceName = deviceName;
            HardwarePort = hardwarePort;
        }

        public string Name { get;  } 

        public string DeviceName { get;  }

        public string HardwarePort { get; }

        public NetworkInterfaceProxySetting? ProxySetting { get; set; }

        
        
        /// <summary>
        /// Mapping between device name and hardward port 
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static Dictionary<string, string> ParseHardwarePortMapping(string[] lines)
        {
            var regexDeviceName = @"Hardware Port: (?<hardwarePort>.*), Device: (?<deviceName>[a-zA-Z0-9_]+)\)$";

            var result = new Dictionary<string, string>();
            
            foreach (var line in lines) {
                var matchResult = Regex.Match(line, regexDeviceName);

                if (!matchResult.Success)
                    continue; 
                
                var deviceName = matchResult.Groups["deviceName"].Value;
                var hardwarePort = matchResult.Groups["hardwarePort"].Value;
                
                result[deviceName] = hardwarePort;
            }
            return result; 
        }
    }

    internal class NetworkInterfaceProxySetting
    {
        private NetworkInterfaceProxySetting(bool enabled, string server, int port)
        {
            Enabled = enabled;
            Server = server;
            Port = port;
        }

        public bool Enabled { get;  }

        public string Server { get; }

        public int Port { get;  }
        
        public string[] ByPassDomains { get; set; } = new string[0];

        /// <summary>
        /// Parses result of command line "networksetup -getwebproxy {interfaceName}"
        /// </summary>
        /// <param name="commandLineResult"></param>
        /// <returns></returns>
        public static NetworkInterfaceProxySetting? ParseFromCommandLineResult(string commandLineResult)
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

            return new NetworkInterfaceProxySetting(enabled, server, port);

        }
    }
}
