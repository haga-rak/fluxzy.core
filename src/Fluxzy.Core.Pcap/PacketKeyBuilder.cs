// Copyright © 2022 Haga Rakotoharivelo

using System.Net;
using System.Runtime.CompilerServices;

namespace Fluxzy.Core.Pcap
{
    /// <summary>
    ///     Packet key builder aims to provide a 64 bits integer value to identify tcp streams and authority.
    /// </summary>
    public static class PacketKeyBuilder
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetConnectionKey(int localPort, int remotePort, IPAddress remoteAddress)
        {
            var portCombination = (remotePort << 16) | localPort;
            var addressHash = remoteAddress.Get4BytesHash(); // TODO : Upgrade to 64 bits hash

            return ((long) portCombination << 32) | (uint) addressHash.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetAuthorityKey(IPAddress address, int port)
        {
            var addressHash = address.Get4BytesHash(); // TODO : Upgrade to 64 bits hash to cover IPv6

            return ((long) port << 32) | (uint) addressHash.GetHashCode();
        }
    }
}
