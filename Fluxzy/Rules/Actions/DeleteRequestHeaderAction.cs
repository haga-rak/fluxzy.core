// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class DeleteRequestHeaderAction : IAction
    {
        public DeleteRequestHeaderAction(string headerName)
        {
            HeaderName = headerName;
        }

        public string HeaderName { get; set;  }

        public FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public Task Alter(ExchangeContext context, Exchange exchange, Connection connection)
        {
            exchange.Request.Header.AltDeleteHeader(HeaderName);
            return Task.CompletedTask;
        }
    }
}