// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    /// Update and existing request header 
    /// </summary>
    public class UpdateRequestHeaderAction : Action
    {
        public UpdateRequestHeaderAction(string headerName, string headerValue)
        {
            HeaderName = headerName;
            HeaderValue = headerValue;
        }

        public string HeaderName { get; set;  }

        public string HeaderValue { get; set;  }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override ValueTask Alter(ExchangeContext context, Exchange? exchange, Connection? connection)
        {
            exchange.Request.Header.AltReplaceHeaders(
                HeaderName, HeaderValue);

            return default;
        }

        public override string DefaultDescription => $"Update request header {HeaderName}".Trim();
    }
}