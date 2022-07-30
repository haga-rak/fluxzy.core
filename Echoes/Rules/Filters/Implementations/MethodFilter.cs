using System.Collections.Generic;
using Echoes.Clients;

namespace Echoes.Rules.Filters.Implementations
{
    public class MethodFilter : StringFilter
    {
        protected override IEnumerable<string> GetMatchInput(Exchange exchange)
        {
            yield return exchange.Request.Header.Path.ToString();
        }

        protected override bool InternalApply(Exchange exchange)
        {
            CaseSensitive = false;
            return base.InternalApply(exchange);
        }
        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;
    }
}