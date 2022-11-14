// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    /// Remove request headers. This actions remove <b>every</b> occurrence of the header from the request
    /// </summary>
    public class DeleteRequestHeaderAction : Action
    {
        public DeleteRequestHeaderAction(string headerName)
        {
            HeaderName = headerName;
        }

        /// <summary>
        /// Header name
        /// </summary>
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