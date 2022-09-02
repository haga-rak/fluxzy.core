// Copyright © 2022 Haga Rakotoharivelo

using System;

namespace Fluxzy
{
    public class ProxyBindPoint : IEquatable<ProxyBindPoint>
    {
        public bool Equals(ProxyBindPoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Address == other.Address && Port == other.Port && Default == other.Default;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ProxyBindPoint)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Address, Port, Default);
        }

        public ProxyBindPoint(string address, int port)
        {
            Address = address;
            Port = port;
        }

        public ProxyBindPoint(string address, int port, bool @default)
        {
            Address = address;
            Port = port;
            Default = @default;
        }

        /// <summary>
        /// The address on with the proxy will listen to 
        /// </summary>
        public string Address { get;  }

        /// <summary>
        /// Port number 
        /// </summary>
        public int Port { get;  }

        /// <summary>
        /// Whether this setting is the default bound address port. When true,
        /// this setting will be choosed as system proxy
        /// </summary>
        public bool Default { get; set; }
    }
}