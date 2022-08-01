using System.Collections.Generic;

namespace Fluxzy.Rules.Filters.RequestFilters
{
    public class MethodFilter : StringFilter
    {
        public MethodFilter(string pattern) : base(pattern)
        {
        }

        public MethodFilter(string pattern, StringSelectorOperation operation) : base(pattern, operation)
        {
        }

        protected override IEnumerable<string> GetMatchInputs(IAuthority authority, IExchange exchange)
        {
            yield return exchange.Method;
        }

        protected override bool InternalApply(IAuthority authority, IExchange exchange)
        {
            CaseSensitive = false;
            return base.InternalApply(authority, exchange);
        }
        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string FriendlyName => $"Request method {base.FriendlyName}";

    }
}