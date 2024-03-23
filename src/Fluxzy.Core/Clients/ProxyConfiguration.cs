// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Clients
{
    /// <summary>
    /// Represents the configuration for a proxy server.
    /// </summary>
    public class ProxyConfiguration
    {
        public ProxyConfiguration(string host, int port, string? proxyAuthorizationHeader = null)
        {
            Host = host;
            Port = port;
            ProxyAuthorizationHeader = proxyAuthorizationHeader;
        }

        /// <summary>
        /// Represents the host information for a proxy server.
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// Represents the port number for a proxy server.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Represents the Proxy Authorization header for a proxy server.
        /// </summary>
        /// <value>
        /// The Proxy Authorization header value.
        /// </value>
        public string? ProxyAuthorizationHeader { get; set; }
    }
}
