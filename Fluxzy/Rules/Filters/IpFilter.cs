// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;

namespace Fluxzy.Rules.Filters
{
    public class IpFilter : StringFilter
    {
        public IpFilter(string pattern) : base(pattern)
        {
        }

        public IpFilter(string pattern, StringSelectorOperation operation) : base(pattern, operation)
        {
        }

        public override FilterScope FilterScope => FilterScope.OnAuthorityReceived;

        protected override IEnumerable<string> GetMatchInputs(IAuthority authority, IExchange exchange)
        {
            yield return exchange.EgressIp;
        }

    }
}