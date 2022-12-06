// Copyright © 2022 Haga RAKOTOHARIVELO

using System.Security.Authentication;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    /// Force the usage of a specific TLS version. Values can be chosen among : Tls, Tls11, Tls12, Tls13 (if .NET 6+), Ssl3, Ssl2.
    /// Forcing the usage of a specific TLS version can break the exchange if the remote does not support the requested protocol.
    /// </summary>
    [ActionMetadata("Force the usage of a specific TLS version. Values can be chosen among : Tls, Tls11, Tls12, Tls13 (if .NET 6+), Ssl3, Ssl2. <br/>" +
                    "Forcing the usage of a specific TLS version can break the exchange if the remote does not support the requested protocol.\r\n    /// </summary>")]
    public class ForceTlsVersionAction : Action
    {
        public ForceTlsVersionAction(SslProtocols sslProtocols)
        {
            SslProtocols = sslProtocols;
        }

        /// <summary>
        /// SslProtocols : Values can be chosen among : Tls, Tls11, Tls12, Tls13 (if .NET 6+), Ssl3, Ssl2. 
        /// </summary>
        public SslProtocols SslProtocols { get; set; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => $"Force using {SslProtocols}";


        public override ValueTask Alter(ExchangeContext context, Exchange? exchange, Connection? connection)
        {
            context.ProxyTlsProtocols = SslProtocols;

            return default; 
        }
    }
}
