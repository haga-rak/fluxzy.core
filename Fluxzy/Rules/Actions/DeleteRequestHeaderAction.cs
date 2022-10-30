// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class DeleteRequestHeaderAction : Action
    {
        public DeleteRequestHeaderAction(string headerName)
        {
            HeaderName = headerName;
        }

        public string HeaderName { get; set;  }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override ValueTask Alter(ExchangeContext context, Exchange? exchange, Connection? connection)
        {
            exchange.Request.Header.AltDeleteHeader(HeaderName);
            return default;
        }

        public override string DefaultDescription => $"Remove header {HeaderName}".Trim();
    }
}