using System.Collections.Generic;
using Echoes.Clients;

namespace Echoes.Rules.Filters.RequestFilters
{
    public class AuthorityFilter : StringFilter
    {
        protected override IEnumerable<string> GetMatchInputs(IExchange exchange)
        {
            yield return exchange.KnownAuthority;
        }

        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string FriendlyName => $"Authority {base.FriendlyName}";
    }
}