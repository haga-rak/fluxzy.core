// Copyright © 2022 Haga Rakotoharivelo

using System.Net;

namespace Fluxzy.Core.Pcap
{
    /// <summary>
    ///     Packet key builder aims to provide a 64 bits integer value to identify tcp streams and authority.
    /// </summary>
    public static class PacketKeyBuilder
    {
        public static long GetConnectionKey(int localPort, int remotePort, IPAddress remoteAddress)
        {
            var portCombination = (remotePort << 16) | localPort;
            var addressHash = remoteAddress.GetHashCode(); // TODO : Upgrade to 64 bits hash

            return ((long) portCombination << 32) | (uint) addressHash.GetHashCode();
        }

        public static long GetAuthorityKey(IPAddress address, int port)
        {
            var addressHash = address.GetHashCode(); // TODO : Upgrade to 64 bits hash to cover IPv6

            return ((long) port << 32) | (uint) addressHash.GetHashCode();
        }
    }
}
