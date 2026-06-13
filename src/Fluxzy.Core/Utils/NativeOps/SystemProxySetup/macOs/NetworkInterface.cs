// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fluxzy.Utils.NativeOps.SystemProxySetup.macOs
{
    internal class NetworkInterface
    {
        public NetworkInterface(string deviceName, string serviceName, string hardwarePort)
        {
            DeviceName = deviceName;
            ServiceName = serviceName;
            HardwarePort = hardwarePort;
        }

        /// <summary>BSD device name (e.g. <c>en0</c>).</summary>
        public string DeviceName { get; }

        /// <summary>
        /// Network service name (e.g. <c>Wi-Fi</c>). This is the identifier every
        /// <c>networksetup</c> proxy sub-command expects - not the device name.
        /// </summary>
        public string ServiceName { get; }

        /// <summary>Hardware port (e.g. <c>Wi-Fi</c>), used only as a stable key for priority ordering.</summary>
        public string HardwarePort { get; }

        public NetworkInterfaceProxySetting? ProxySetting { get; set; }

        private static readonly Regex ServiceHeaderRegex =
            new(@"^\((?:\d+|\*)\)\s+(?<service>.+)$");

        private static readonly Regex DeviceLineRegex =
            new(@"^\(Hardware Port:\s*(?<hardwarePort>.*),\s*Device:\s*(?<deviceName>[a-zA-Z0-9_]+)\)$");

        /// <summary>
        /// Parses <c>networksetup -listnetworkserviceorder</c>, pairing each "(n) ServiceName"
        /// header with the "(Hardware Port: ..., Device: ...)" line that follows it.
        /// </summary>
        public static IReadOnlyList<NetworkInterface> ParseNetworkServices(IEnumerable<string> lines)
        {
            var result = new List<NetworkInterface>();
            string? currentService = null;

            foreach (var rawLine in lines) {
                var line = rawLine.Trim();

                var header = ServiceHeaderRegex.Match(line);

                if (header.Success) {
                    currentService = header.Groups["service"].Value.Trim();
                    continue;
                }

                var device = DeviceLineRegex.Match(line);

                if (device.Success && currentService != null) {
                    result.Add(new NetworkInterface(
                        device.Groups["deviceName"].Value,
                        currentService,
                        device.Groups["hardwarePort"].Value.Trim()));

                    currentService = null;
                }
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

        public string[] ByPassDomains { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Parses result of command line "networksetup -getsecurewebproxy {serviceName}"
        /// </summary>
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
