// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;
using Echoes.Clients;

namespace Echoes.Rules.Filters.RequestFilters
{
    public class FullUrlFilter : StringFilter
    {
        protected override IEnumerable<string> GetMatchInput(Exchange exchange)
        {
            yield return exchange.Request.Header.GetFullUrl();
        }

        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;
    }
}