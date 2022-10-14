// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Fluxzy.Rules.Filters.ResponseFilters
{
    public class ResponseHeaderFilter : HeaderFilter
    {
        protected override IEnumerable<string> GetMatchInputs(IAuthority authority, IExchange exchange)
        {
            return exchange.GetResponseHeaders().Where(e =>
                    e.Name.Span.Equals(HeaderName.AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                .Select(s => s.Value.ToString());
        }

        public override FilterScope FilterScope => FilterScope.ResponseHeaderReceivedFromRemote;

        public ResponseHeaderFilter(string pattern, string headerName) : base(pattern, headerName)
        {
        }

        [JsonConstructor]
        public ResponseHeaderFilter(string pattern, StringSelectorOperation operation, string headerName) : base(pattern, operation, headerName)
        {
        }

        public override string GenericName => "Filter by response header";
    }
}