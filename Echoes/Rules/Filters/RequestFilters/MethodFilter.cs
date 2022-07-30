using System.Collections.Generic;
using Echoes.Clients;

namespace Echoes.Rules.Filters.RequestFilters
{
    public class MethodFilter : StringFilter
    {
        protected override IEnumerable<string> GetMatchInputs(IExchange exchange)
        {
            yield return exchange.Method;
        }

        protected override bool InternalApply(IExchange exchange)
        {
            CaseSensitive = false;
            return base.InternalApply(exchange);
        }
        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;
    }
}