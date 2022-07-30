// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;
using Echoes.Clients;

namespace Echoes.Rules.Filters.ResponseFilters
{
    public class StatusCodeFilter : Filter
    {
        protected override bool InternalApply(IExchange exchange)
        {
            return StatusCodes.Contains(exchange.StatusCode); 
        }

        public List<int> StatusCodes { get; set; } = new();

        public override FilterScope FilterScope => FilterScope.ResponseHeaderReceivedFromRemote;
    }
}