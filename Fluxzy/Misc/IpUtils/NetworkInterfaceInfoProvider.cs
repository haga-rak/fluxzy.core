using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Fluxzy.Misc.IpUtils
{
    public static class NetworkInterfaceInfoProvider
    {
        public static List<NetworkInterfaceInfo> GetNetworkInterfaceInfos()
        {
            var allInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            return allInterfaces.
                   SelectMany(i => i.GetIPProperties().UnicastAddresses.Select(a => new {
                       a.Address,
                       i.Name
                   }))
                   .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork ||
                               a.Address.AddressFamily == AddressFamily.InterNetworkV6)
                   .Where(a => !a.Address.IsIPv6LinkLocal
                               && a.Address.AddressFamily == AddressFamily.InterNetwork)
                   .Select(a => new NetworkInterfaceInfo(a.Address, a.Name))
                   .OrderBy(a => !a.IPAddress.Equals(IPAddress.Loopback))
                   .ToList();
        }
    }

    public class NetworkInterfaceInfo
    {
        public NetworkInterfaceInfo(IPAddress ipAddress, string interfaceName)
        {
            IPAddress = ipAddress;
            InterfaceName = interfaceName;

            if (ipAddress.Equals(IPAddress.Loopback))
            {
                InterfaceName = "Loopback";
            }
        }

        public IPAddress IPAddress { get; }

        public string InterfaceName { get; }
    }
}
