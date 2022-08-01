// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;
using System.Linq;

namespace Fluxzy.Rules.Filters.ResponseFilters
{
    public class StatusCodeFilter : Filter
    {
        protected override bool InternalApply(IAuthority authority, IExchange exchange)
        {
            return StatusCodes.Contains(exchange.StatusCode); 
        }

        public List<int> StatusCodes { get; set; } = new();

        public override FilterScope FilterScope => FilterScope.ResponseHeaderReceivedFromRemote;


        public override string FriendlyName => $"Status code among {string.Join(", ", StatusCodes.Select(s => s.ToString()))}";
    }
}