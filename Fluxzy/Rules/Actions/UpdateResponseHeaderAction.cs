// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class UpdateResponseHeaderAction : IAction
    {
        public UpdateResponseHeaderAction(string headerName, string headerValue)
        {
            HeaderName = headerName;
            HeaderValue = headerValue;
        }

        public string HeaderName { get; set;  }

        public string HeaderValue { get; set;  }

        public FilterScope ActionScope => FilterScope.ResponseHeaderReceivedFromRemote;

        public Task Alter(ExchangeContext context, Exchange exchange, Connection connection)
        {
            exchange.Response.Header.AltReplaceHeaders(
                HeaderName, HeaderValue);

            return Task.CompletedTask;
        }
    }
}