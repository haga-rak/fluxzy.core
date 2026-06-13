// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using Fluxzy.Core;

namespace Fluxzy.Utils.ProcessTracking
{
    /// <summary>
    /// Describes a downstream client connection passed to an <see cref="IProcessInfoResolver"/>
    /// so the originating process can be resolved.
    /// </summary>
    public sealed class ProcessResolutionContext
    {
        public ProcessResolutionContext(
            IPEndPoint remoteEndPoint, IPEndPoint localEndPoint, Authority requestedAuthority)
        {
            RemoteEndPoint = remoteEndPoint;
            LocalEndPoint = localEndPoint;
            RequestedAuthority = requestedAuthority;
        }

        /// <summary>
        /// The downstream client endpoint seen by the proxy. When a transparent tunnel such as
        /// tun2socks fronts the proxy, this is the tunnel socket, not the real client.
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; }

        /// <summary>
        /// The proxy endpoint that accepted the connection.
        /// </summary>
        public IPEndPoint LocalEndPoint { get; }

        /// <summary>
        /// The destination requested by the client, that is the CONNECT or SOCKS5 target.
        /// </summary>
        public Authority RequestedAuthority { get; }
    }
}
