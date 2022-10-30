// Copyright © 2022 Haga Rakotoharivelo

using System;

namespace Fluxzy.Clients
{
    /// <summary>
    ///     Hold information about a hostname and a port number
    /// </summary>
    public readonly struct Authority : IEquatable<Authority>, IAuthority
    {
        public Authority(string hostName, int port, bool secure)
        {
            HostName = hostName;
            Port = port;
            Secure = secure;
        }

        /// <summary>
        ///     Hostname
        /// </summary>
        public string HostName { get; }

        /// <summary>
        ///     Port number
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// </summary>
        public bool Secure { get; }

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

        public override string ToString()
        {
            return $"{HostName}:{Port}";
        }
    }
}
