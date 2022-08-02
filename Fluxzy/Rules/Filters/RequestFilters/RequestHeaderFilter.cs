// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Linq;

namespace Fluxzy.Rules.Filters.RequestFilters
{
    public class RequestHeaderFilter : HeaderFilter
    {
        public RequestHeaderFilter(string pattern, string headerName)
            : base(pattern, headerName)
        {
        }

        public RequestHeaderFilter(string pattern, StringSelectorOperation operation, string headerName) : base(pattern, operation, headerName)
        {
        }
        protected override IEnumerable<string> GetMatchInputs(IAuthority authority, IExchange exchange)
        {
            return exchange.GetRequestHeaders().Where(e =>
                    MemoryExtensions.Equals(e.Name.Span, HeaderName.AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                .Select(s => s.Value.ToString());
        }

        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;

    }
}