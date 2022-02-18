// Copyright © 2022 Haga Rakotoharivelo

using System;

namespace Echoes
{
    public readonly struct Authority : IEquatable<Authority>
    {
        public Authority(string hostName, int port, bool secure)
        {
            HostName = hostName;
            Port = port;
            Secure = secure;
        }

        public bool Empty => HostName == null && Port == 0;  

        public string HostName { get;  }

        public int Port { get;  }

        public bool Equals(Authority other)
        {
            return
                string.Equals(HostName, other.HostName, StringComparison.OrdinalIgnoreCase)
                && Port == other.Port && Secure == other.Secure;
        }

        public override bool Equals(object obj)
        {
            return obj is Authority other && Equals(other);
        }

        public override int GetHashCode()
        {
            Span<char> destBuffer = stackalloc char[HostName.Length];
            return HashCode.Combine(HostName.AsSpan().ToLowerInvariant(destBuffer), Port, Secure);
        }

        public bool Secure { get;  }
    }
}