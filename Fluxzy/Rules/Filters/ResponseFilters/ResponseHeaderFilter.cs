﻿// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Fluxzy.Rules.Filters.ResponseFilters
{
    /// <summary>
    /// Select exchange according to response header values.
    /// </summary>
    /// 
    [FilterMetaData(
        LongDescription = "Select exchange according to response header values."
    )]
    public class ResponseHeaderFilter : HeaderFilter
    {
        public override FilterScope FilterScope => FilterScope.ResponseHeaderReceivedFromRemote;

        public override string ShortName => "resp head.";

        public virtual string GenericName => "Filter by response header";

        public ResponseHeaderFilter(string pattern, string headerName)
            : base(pattern, headerName)
        {
        }

        [JsonConstructor]
        public ResponseHeaderFilter(string pattern, StringSelectorOperation operation, string headerName)
            : base(pattern, operation, headerName)
        {
        }

        protected override IEnumerable<string> GetMatchInputs(IAuthority authority, IExchange? exchange)
        {
            return exchange?.GetResponseHeaders()?.Where(e =>
                               e.Name.Span.Equals(HeaderName.AsSpan(), StringComparison.InvariantCultureIgnoreCase))
                           .Select(s => s.Value.ToString()) ?? Array.Empty<string>();
        }
    }
}
