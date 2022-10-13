// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Fluxzy.Rules.Filters.RequestFilters
{
    public class RequestHeaderFilter : HeaderFilter
    {
        public RequestHeaderFilter(string pattern, string headerName)
            : base(pattern, headerName)
        {
        }

        [JsonConstructor]
        public RequestHeaderFilter(string pattern, StringSelectorOperation operation, string headerName) : base(pattern, operation, headerName)
        {
        }
        protected override IEnumerable<string> GetMatchInputs(IAuthority authority, IExchange exchange)
        {
            return exchange.GetRequestHeaders().Where(e =>
                    e.Name.Span.Equals(HeaderName.AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                .Select(s => s.Value.ToString());
        }

        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;

    }
}