// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Fluxzy.Misc
{
    public static class IpUtility
    {
        public static HashSet<IPAddress> LocalAddresses { get; } = GetAllLocalIp();

        internal static HashSet<IPAddress> GetAllLocalIp()
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
