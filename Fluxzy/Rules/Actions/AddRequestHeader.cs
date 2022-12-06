// Copyright © 2022 Haga RAKOTOHARIVELO

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.Headers;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{

    /// <summary>
    /// Append a request header.
    /// <strong>Note</strong> Headers that alter the connection behaviour will be ignored.
    /// </summary>
    [ActionMetadata("Append a request header.")]
    public class AddRequestHeaderAction : Action
    {
        /// <summary>
        /// Header name 
        /// </summary>
        public string HeaderName { get; set; }

        /// <summary>
        /// Header value
        /// </summary>
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
            context.RequestHeaderAlterations.Add(new HeaderAlterationAdd(HeaderName, HeaderValue));
            
            return default;
        }
    }
}
