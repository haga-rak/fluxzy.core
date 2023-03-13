// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.Headers;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    /// <summary>
    ///     Remove response headers. This actions remove <b>every</b> occurrence of the header from the response.
    /// </summary>
    [ActionMetadata(
        "Remove response headers. This actions remove <b>every</b> occurrence of the header from the response.")]
    public class DeleteResponseHeaderAction : Action
    {
        public DeleteResponseHeaderAction(string headerName)
        {
            HeaderName = headerName;
        }

        /// <summary>
        ///     Header name
        /// </summary>
        public string HeaderName { get; set; }

        public override FilterScope ActionScope => FilterScope.ResponseHeaderReceivedFromRemote;

        public override string DefaultDescription => $"Remove response header {HeaderName}".Trim();

        public override ValueTask Alter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope)
        {
            context.ResponseHeaderAlterations.Add(new HeaderAlterationDelete(HeaderName));

            return default;
        }
    }
}
