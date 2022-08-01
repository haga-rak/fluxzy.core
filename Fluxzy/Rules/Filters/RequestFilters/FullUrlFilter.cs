// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;

namespace Fluxzy.Rules.Filters.RequestFilters
{
    public class FullUrlFilter : StringFilter
    {
        public FullUrlFilter(string pattern) : base(pattern)
        {

        }

        public FullUrlFilter(string pattern, StringSelectorOperation operation) : base(pattern, operation)
        {

        }

        protected override IEnumerable<string> GetMatchInputs(IAuthority authority, IExchange exchange)
        {
            yield return exchange.FullUrl;
        }

        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string FriendlyName => $"Full url {base.FriendlyName}";

    }
}