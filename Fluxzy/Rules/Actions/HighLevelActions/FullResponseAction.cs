// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.Mock;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions.HighLevelActions
{
    /// <summary>
    ///     Mock completely a response.
    /// </summary>
    public class FullResponseAction : Action
    {
        public FullResponseAction(PreMadeResponse preMadeResponse)
        {
            PreMadeResponse = preMadeResponse;
        }

        /// <summary>
        ///     The response
        /// </summary>
        public PreMadeResponse PreMadeResponse { get; set; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => "Full response substitution";

        public override ValueTask Alter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope,
            BreakPointManager breakPointManager)
        {
            context.PreMadeResponse = PreMadeResponse;

            return default;
        }
    }
}
