// Copyright © 2022 Haga Rakotoharivelo

using Echoes.Clients;

namespace Echoes.Rules.Filters.ResponseFilters
{
    public class StatusCodeSuccess : Filter
    {
        protected override bool InternalApply(IExchange exchange)
        {
            var statusCode = exchange.StatusCode;
            return statusCode is >= 200 and < 300; 
        }
        public override FilterScope FilterScope => FilterScope.ResponseHeaderReceivedFromRemote;
    }
}