using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fluxzy.Rules.Filters.RequestFilters
{
    public class HostFilter : StringFilter
    {
        public HostFilter(string pattern) : base(pattern)
        {

        }

        [JsonConstructor]
        public HostFilter(string pattern, StringSelectorOperation operation) : base(pattern, operation)
        {
        }

        protected override IEnumerable<string> GetMatchInputs(IAuthority authority, IExchange exchange)
        {
            yield return authority.HostName;
        }

        public override FilterScope FilterScope => FilterScope.OnAuthorityReceived;

        public override string FriendlyName => $"Authority {base.FriendlyName}";

    }
}