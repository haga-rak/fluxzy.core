// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net;
using System.Net.Sockets;

namespace Fluxzy.Core
{
    /// <summary>
    ///     Invoked once per upstream socket, after the socket is created and before it connects.
    /// </summary>
    public delegate void ConfigureUpstreamSocket(UpstreamSocketContext context);

    /// <summary>
    ///     Context handed to a <see cref="ConfigureUpstreamSocket"/> callback.
    /// </summary>
    public sealed class UpstreamSocketContext
    {
        public UpstreamSocketContext(Socket socket, string requestedHost, int requestedPort, IPEndPoint remoteEndPoint)
        {
            Socket = socket;
            RequestedHost = requestedHost;
            RequestedPort = requestedPort;
            RemoteEndPoint = remoteEndPoint;
        }

        /// <summary>
        ///     The upstream socket, not yet connected. Safe to set options or bind.
        /// </summary>
        public Socket Socket { get; }

        /// <summary>
        ///     Host (domain or IP literal) as requested by the client.
        /// </summary>
        public string RequestedHost { get; }

        /// <summary>
        ///     Port as requested by the client.
        /// </summary>
        public int RequestedPort { get; }

        /// <summary>
        ///     The endpoint fluxzy will connect to. The upstream proxy when chaining, otherwise the destination.
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; }
    }

    /// <summary>
    ///     Carries the requested authority and the optional socket configuration callback down to the
    ///     connection implementation.
    /// </summary>
    public sealed class UpstreamConnectOptions
    {
        public static readonly UpstreamConnectOptions None = new(string.Empty, 0, null);

        public UpstreamConnectOptions(string requestedHost, int requestedPort, ConfigureUpstreamSocket? configure)
        {
            RequestedHost = requestedHost;
            RequestedPort = requestedPort;
            Configure = configure;
        }

        public string RequestedHost { get; }

        public int RequestedPort { get; }

        public ConfigureUpstreamSocket? Configure { get; }

        public void Apply(Socket socket, IPEndPoint remoteEndPoint)
        {
            if (Configure == null)
                return;

            try {
                Configure(new UpstreamSocketContext(socket, RequestedHost, RequestedPort, remoteEndPoint));
            }
            catch (ClientErrorException) {
                throw;
            }
            catch (Exception ex) {
                throw new ClientErrorException(-1, "Upstream socket configuration callback failed",
                    innerException: ex, networkErrorCode: NetworkErrorCodes.UpstreamConfigurationFailed);
            }
        }
    }
}
