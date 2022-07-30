﻿using Echoes.Clients;

namespace Echoes.Rules.Filters.ResponseFilters
{
    public class StatusCodeRedirectionFilter : Filter
    {
        protected override bool InternalApply(IExchange exchange)
        {
            var statusCode = exchange.StatusCode;
            return statusCode is >= 300 and < 400; 
        }
        public override FilterScope FilterScope => FilterScope.ResponseHeaderReceivedFromRemote;
    }
}