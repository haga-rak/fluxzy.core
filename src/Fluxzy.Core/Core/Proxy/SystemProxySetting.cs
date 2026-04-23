// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

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

        /// <summary>
        ///     True only if the OS proxy is enabled AND its host/port match the given endpoint.
        ///     Used to distinguish "Fluxzy is the active system proxy" from "some other proxy is on".
        /// </summary>
        public bool MatchesEndPoint(IPEndPoint endPoint)
        {
            if (!Enabled)
                return false;

            if (ListenPort != endPoint.Port)
                return false;

            return string.Equals(BoundHost, endPoint.Address.ToString(),
                StringComparison.OrdinalIgnoreCase);
        }

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
            return HashCode.Combine(BoundHost, ListenPort);
        }
    }
}
