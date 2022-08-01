// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Rules.Filters.ResponseFilters
{
    public class StatusCodeSuccess : Filter
    {
        protected override bool InternalApply(IAuthority authority, IExchange exchange)
        {
            var statusCode = exchange.StatusCode;
            return statusCode is >= 200 and < 300; 
        }
        public override FilterScope FilterScope => FilterScope.ResponseHeaderReceivedFromRemote;

        public override string FriendlyName => $"Success status code (2XX)";
    }
}