// Copyright © 2022 Haga Rakotoharivelo

using System;
using Fluxzy.Misc;

namespace Fluxzy.Rules.Filters.RequestFilters
{
    public class H2TrafficOnlyFilter : Filter
    {
        protected override bool InternalApply(IAuthority? authority, IExchange? exchange,
            IFilteringContext? filteringContext)
        {
            return exchange?.HttpVersion == "HTTP/2";
        }

        public override Guid Identifier => $"{GetType().Name}{Inverted}".GetMd5Guid();

        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string GenericName => "HTTP/2 only";

        public override string ShortName => "h2";
        public override bool PreMadeFilter => true;
    }
}