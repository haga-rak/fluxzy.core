// Copyright © 2022 Haga RAKOTOHARIVELO

using System.Security.Authentication;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class ForceTlsVersionAction : Action
    {
        public ForceTlsVersionAction(SslProtocols sslProtocols)
        {
            SslProtocols = sslProtocols;
        }

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
