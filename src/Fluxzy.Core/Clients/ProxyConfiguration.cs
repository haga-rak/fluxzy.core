// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Net.Http;

namespace Fluxzy.Clients
{
    /// <summary>
    /// Represents the configuration for a proxy server.
    /// </summary>
    public class ProxyConfiguration
    {
        private HttpMessageHandler? _clientHandler;

        public ProxyConfiguration(string host, int port, NetworkCredential? credentials)
        {
            Host = host;
            Port = port;
            Credentials = credentials;

            if (credentials != null) {
                ProxyAuthorizationHeader = BasicAuthenticationHelper.GetBasicAuthHeader(credentials);
            }
        }

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
        ///  Network credentials for a proxy server.
        /// </summary>
        public NetworkCredential? Credentials { get; }

        /// <summary>
        /// Represents the Proxy Authorization header for a proxy server.
        /// </summary>
        /// <value>
        /// The Proxy Authorization header value.
        /// </value>
        public string? ProxyAuthorizationHeader { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal HttpMessageHandler GetDefaultHandler()
        {
            if (_clientHandler != null) {
                return _clientHandler;
            }

            lock (this) {

                var clientHandler = new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = (_, certificate2, arg3, arg4) => true,
                };

                var webProxy = new WebProxy(Host, Port);

                if (Credentials != null)
                {
                    webProxy.Credentials = Credentials;
                }

                clientHandler.Proxy = webProxy;
                clientHandler.UseProxy = true;

                _clientHandler = clientHandler;

                return clientHandler;
            }
        }
    }
}
