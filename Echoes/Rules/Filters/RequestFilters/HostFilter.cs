using System.Collections.Generic;
using Echoes.Clients;

namespace Echoes.Rules.Filters.RequestFilters
{
    public class HostFilter : StringFilter
    {
        protected override IEnumerable<string> GetMatchInput(Exchange exchange)
        {
            yield return exchange.Request.Header.Authority.ToString();
        }

        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;
    }
}