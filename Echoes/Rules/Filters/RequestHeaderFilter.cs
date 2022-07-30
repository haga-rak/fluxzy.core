// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Linq;
using Echoes.Clients;

namespace Echoes.Rules.Filters
{
    public abstract class HeaderFilter : StringFilter
    {
        public string HeaderName { get; set; }
    }

    public class RequestHeaderFilter : HeaderFilter
    {
        protected override IEnumerable<string> GetMatchInput(Exchange exchange)
        {
            return exchange.Request.Header.Headers.Where(e =>
                    MemoryExtensions.Equals(e.Name.Span, HeaderName.AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                .Select(s => s.Value.ToString());
        }

        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;
    }

    public class ResponseHeaderFilter : HeaderFilter
    {
        protected override IEnumerable<string> GetMatchInput(Exchange exchange)
        {
            return exchange.Response.Header.Headers.Where(e =>
                    MemoryExtensions.Equals(e.Name.Span, HeaderName.AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                .Select(s => s.Value.ToString());
        }

        public override FilterScope FilterScope => FilterScope.ResponseHeaderReceivedFromRemote;
    }
}