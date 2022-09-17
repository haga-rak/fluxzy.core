// Copyright © 2022 Haga Rakotoharivelo

using System.Net;

namespace Fluxzy.Interop.Pcap
{
    internal readonly struct Authority : IEquatable<Authority>
    {
        public Authority(IPAddress address, int port)
        {
            Address = address;
            Port = port;
        }

        public IPAddress Address { get; }

        public int Port { get; }

        public bool Equals(Authority other)
        {
            return Address.Equals(other.Address) && Port == other.Port;
        }

        public override bool Equals(object? obj)
        {
            return obj is Authority other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Address, Port);
        }

    }
}