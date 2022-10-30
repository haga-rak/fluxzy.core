// Copyright © 2022 Haga RAKOTOHARIVELO

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class AddRequestHeaderAction : Action
    {
        public string HeaderName { get; set; }

        public string HeaderValue { get; set; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription =>
            string.IsNullOrWhiteSpace(HeaderName)
                ? "Add request header"
                : $"Add request header ({HeaderName}, {HeaderValue})";

        public AddRequestHeaderAction(string headerName, string headerValue)
        {
            HeaderName = headerName;
            HeaderValue = headerValue;
        }

        public override ValueTask Alter(ExchangeContext context, Exchange? exchange, Connection? connection)
        {
            exchange.Request.Header.AltAddHeader(
                HeaderName,
                HeaderValue
            );

            return default;
        }
    }
}
