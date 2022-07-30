using System.Collections.Generic;
using Echoes.Clients;

namespace Echoes.Rules.Filters.RequestFilters
{
    public class PathFilter : StringFilter
    {
        protected override IEnumerable<string> GetMatchInput(Exchange exchange)
        {
            yield return exchange.Request.Header.Path.ToString();
        }
        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;
    }
}