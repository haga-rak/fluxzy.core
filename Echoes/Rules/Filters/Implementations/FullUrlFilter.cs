// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;
using Echoes.Clients;

namespace Echoes.Rules.Filters.Implementations
{
    public class FullUrlFilter : StringFilter
    {
        protected override IEnumerable<string> GetMatchInput(Exchange exchange)
        {
            yield return exchange.Request.Header.GetFullUrl();
        }
    }
}