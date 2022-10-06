// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Net;

namespace Fluxzy
{
    public class ProxyBindPoint : IEquatable<ProxyBindPoint>
    {
        public ProxyBindPoint(IPEndPoint endPoint, bool @default)
        {
            EndPoint = endPoint;
            Default = @default;
        }

        /// <summary>
        ///     Combination of an IP address and port number
        /// </summary>
        public IPEndPoint EndPoint { get; }

        /// <summary>
        ///     Whether this setting is the default bound address port. When true,
        ///     this setting will be choose as system proxy
        /// </summary>
        public bool Default { get; set; }

        public bool Equals(ProxyBindPoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(EndPoint, other.EndPoint) && Default == other.Default;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ProxyBindPoint)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EndPoint, Default);
        }
    }
}