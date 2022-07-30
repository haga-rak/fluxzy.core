using System.Collections.Generic;
using Echoes.Clients;

namespace Echoes.Rules.Filters.RequestFilters
{
    public class PathFilter : StringFilter
    {
        protected override IEnumerable<string> GetMatchInputs(IExchange exchange)
        {
            yield return exchange.Path;
        }
        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;
    }
}