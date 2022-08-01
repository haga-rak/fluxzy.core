// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Linq;

namespace Fluxzy.Rules.Filters.ResponseFilters
{
    public class ResponseHeaderFilter : HeaderFilter
    {
        protected override IEnumerable<string> GetMatchInputs(IExchange exchange)
        {
            return exchange.GetResponseHeaders().Where(e =>
                    e.Name.Span.Equals(HeaderName.AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                .Select(s => s.Value.ToString());
        }

        public override FilterScope FilterScope => FilterScope.ResponseHeaderReceivedFromRemote;
    }
}