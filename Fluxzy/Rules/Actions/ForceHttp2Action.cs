using System.Collections.Generic;
using System.Net.Security;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    /// Force the connection between fluxzy and remote to be HTTP/2.0. This value is enforced by ALPN settings on TLS.
    /// The exchange will break if the remote does not support HTTP/2.0.
    /// This action will be ignored when the communication is clear (H2c not supported)
    /// </summary>
    public class ForceHttp2Action : Action
    {
        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public override string DefaultDescription => $"Force using HTTP/2.0";
        
        public override ValueTask Alter(ExchangeContext context, Exchange? exchange, Connection? connection)
        {
            // TODO avoid allocating new list here 

            context.SslApplicationProtocols = new List<SslApplicationProtocol>()
            {
                SslApplicationProtocol.Http2
            };

            return default; 
        }
    }
}