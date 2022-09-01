// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class DeleteResponseHeaderAction : IAction
    {
        public DeleteResponseHeaderAction(string headerName)
        {
            HeaderName = headerName;
        }

        public string HeaderName { get; set;  }

        public FilterScope ActionScope => FilterScope.ResponseHeaderReceivedFromRemote;

        public Task Alter(ExchangeContext context, Exchange exchange, Connection connection)
        {
            exchange.Response.Header.AltDeleteHeader(HeaderName);
            return Task.CompletedTask;
        }
    }
}