// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;

namespace Echoes.Rules.Filters
{
    public class IpFilter : StringFilter
    {
        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;

        protected override IEnumerable<string> GetMatchInput(IExchange exchange)
        {
            yield return exchange.EgressIp;
        }
    }
}