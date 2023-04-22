// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fluxzy.Rules.Filters.RequestFilters
{
    /// <summary>
    ///     Filter according to full url (includes path and query)
    /// </summary>
    [FilterMetaData(
        LongDescription = "Select exchange to full url (scheme, FQDN, path and query).",
        QuickReachFilter = true,
        QuickReachFilterOrder = 0
    )]
    public class FullUrlFilter : StringFilter
    {
        public FullUrlFilter(string pattern)
            : base(pattern)
        {
        }

        [JsonConstructor]
        public FullUrlFilter(string pattern, StringSelectorOperation operation)
            : base(pattern, operation)
        {
        }

        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string AutoGeneratedName => $"Full url `{Pattern}`";

        public override string ShortName => "url";

        public override bool Common { get; set; } = true;

        protected override IEnumerable<string> GetMatchInputs(IAuthority authority, IExchange? exchange)
        {
            if (exchange != null)
                yield return exchange.FullUrl;
        }
    }
}
