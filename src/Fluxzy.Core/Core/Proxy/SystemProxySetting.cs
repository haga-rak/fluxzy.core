// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;

namespace Fluxzy.Core.Proxy
{
    public class SystemProxySetting : IEquatable<SystemProxySetting>
    {
        public SystemProxySetting(string boundHost, int listenPort, params string[] byPassHosts)
        {
            BoundHost = boundHost;
            ListenPort = listenPort;
            ByPassHosts = byPassHosts.OrderBy(h => h).ToList();
            Enabled = true;
        }

        public string BoundHost { get; }

        public int ListenPort { get; set; }

        public IReadOnlyCollection<string> ByPassHosts { get; }

        /// <summary>
        ///     TODO. make this class immutable and remove setter on this property
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        ///     This is used to store specif OS related proxy format
        /// </summary>
        public Dictionary<string, object> PrivateValues { get; } = new();

        public bool Equals(SystemProxySetting? other)
        {
            if (ReferenceEquals(null, other))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return BoundHost == other.BoundHost && ListenPort == other.ListenPort
                                                &&
                                                ByPassHosts.SequenceEqual(other.ByPassHosts);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((SystemProxySetting) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(BoundHost, ListenPort, ByPassHosts);
        }
    }
}
