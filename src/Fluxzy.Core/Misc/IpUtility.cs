// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Fluxzy.Misc
{
    /// <summary>
    /// An helper class for IP related operations
    /// </summary>
    public static class IpUtility
    {
        /// <summary>
        /// All unicat adresses (IPv4 and IPv6) found on the current computer
        /// </summary>
        public static HashSet<IPAddress> LocalAddresses { get; } = GetAllLocalIps();

        /// <summary>
        /// Get all local IP addresses
        /// </summary>
        /// <returns></returns>
        internal static HashSet<IPAddress> GetAllLocalIps()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                                   .Where(x => 
                                       x.OperationalStatus == OperationalStatus.Up)
                                   .SelectMany(x => x.GetIPProperties().UnicastAddresses)
                                   .Where(x =>
                                       x.Address.AddressFamily == AddressFamily.InterNetwork
                                       || x.Address.AddressFamily == AddressFamily.InterNetworkV6)
                                   .Select(x => x.Address)
                                   .ToHashSet();
        }
    }
}
