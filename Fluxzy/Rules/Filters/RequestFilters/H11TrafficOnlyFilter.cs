// Copyright © 2022 Haga Rakotoharivelo

using System;
using Fluxzy.Misc;

namespace Fluxzy.Rules.Filters.RequestFilters
{
    public class H11TrafficOnlyFilter : Filter
    {
        protected override bool InternalApply(IAuthority authority, IExchange exchange,
            IFilteringContext? filteringContext)
        {
            return exchange.HttpVersion == "HTTP/1.1";
        }

        public override Guid Identifier => $"{GetType().Name}{Inverted}".GetMd5Guid();

        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string GenericName => "HTTP/1.1 only";

        public override string ShortName => "h11";

        public override bool PreMadeFilter => true;
    }
}