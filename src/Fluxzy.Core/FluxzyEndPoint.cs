// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net;
using System.Text.Json.Serialization;

namespace Fluxzy
{
    public class FluxzyEndPoint : IEquatable<FluxzyEndPoint>
    {
        [JsonConstructor]
        public FluxzyEndPoint(string address, int port)
        {
            Address = address;
            Port = port;
        }

        public FluxzyEndPoint(IPEndPoint endPoint)
            : this(endPoint.Address.ToString(), endPoint.Port)
        {
        }

        public string Address { get; set; }

        public int Port { get; set; }

        public bool Equals(FluxzyEndPoint? other)
        {
            if (ReferenceEquals(null, other))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return Address == other.Address && Port == other.Port;
        }

        public IPEndPoint ToIpEndPoint()
        {
            return new IPEndPoint(IPAddress.Parse(Address), Port);
        }

        public static implicit operator IPEndPoint(FluxzyEndPoint d)
        {
            return d.ToIpEndPoint();
        }

        public static implicit operator FluxzyEndPoint(IPEndPoint d)
        {
            return new(d);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((FluxzyEndPoint) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Address, Port);
        }
    }
}
