// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;
using Echoes.Clients;

namespace Echoes.Rules.Filters.Implementations
{
    public class StatusCodeFilter : Filter
    {
        protected override bool InternalApply(Exchange exchange)
        {
            return StatusCodes.Contains(exchange.Response?.Header.StatusCode ?? -1); 
        }

        public List<int> StatusCodes { get; set; } = new(); 
    }
}