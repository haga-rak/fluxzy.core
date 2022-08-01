// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;

namespace Fluxzy.Rules.Filters.RequestFilters
{
    public class FullUrlFilter : StringFilter
    {
        protected override IEnumerable<string> GetMatchInputs(IExchange exchange)
        {
            yield return exchange.FullUrl;
        }

        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string FriendlyName => $"Full url {base.FriendlyName}";
    }
}