﻿// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fluxzy.Rules.Filters
{
    public class IpEgressFilter : StringFilter
    {
        public override FilterScope FilterScope => FilterScope.OnAuthorityReceived;

        public virtual string GenericName => "Filter by Egress IP Address";

        public override string ShortName => "ip";

        public IpEgressFilter(string pattern)
            : base(pattern)
        {
        }

        [JsonConstructor]
        public IpEgressFilter(string pattern, StringSelectorOperation operation)
            : base(pattern, operation)
        {
        }

        protected override IEnumerable<string> GetMatchInputs(IAuthority authority, IExchange? exchange)
        {
            yield return exchange.EgressIp ?? string.Empty;
        }
    }
}
