using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fluxzy.Rules.Filters.RequestFilters
{
    public class PathFilter : StringFilter
    {
        public PathFilter(string pattern) : base(pattern)
        {
        }

        [JsonConstructor]
        public PathFilter(string pattern, StringSelectorOperation operation) : base(pattern, operation)
        {
        }

        protected override IEnumerable<string> GetMatchInputs(IAuthority authority, IExchange exchange)
        {
            yield return exchange.Path;
        }
        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string FriendlyName => $"Request path {base.FriendlyName}";

    }
}