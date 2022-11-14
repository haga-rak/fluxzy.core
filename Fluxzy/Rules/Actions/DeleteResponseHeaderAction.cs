// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    /// Remove response headers. This actions remove <b>every</b> occurrence of the header from the response.
    /// </summary>
    public class DeleteResponseHeaderAction : Action
    {
        public DeleteResponseHeaderAction(string headerName)
        {
            HeaderName = headerName;
        }

        /// <summary>
        /// Header name
        /// </summary>
        public string HeaderName { get; set;  }

        public override FilterScope ActionScope => FilterScope.ResponseHeaderReceivedFromRemote;

        public override ValueTask Alter(ExchangeContext context, Exchange? exchange, Connection? connection)
        {
            exchange.Response.Header?.AltDeleteHeader(HeaderName);
            return default;
        }

        public override string DefaultDescription => $"Remove response header {HeaderName}".Trim();
    }
}