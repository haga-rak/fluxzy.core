// Copyright © 2022 Haga Rakotoharivelo

using System;
using Fluxzy.Misc;

namespace Fluxzy.Rules.Filters.RequestFilters
{
    /// <summary>
    /// Select HTTP/2.0 traffic only
    /// </summary>
    public class H2TrafficOnlyFilter : Filter
    {
        public override Guid Identifier => $"{GetType().Name}{Inverted}".GetMd5Guid();

        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;

        public virtual string GenericName => "HTTP/2 only";

        public override string ShortName => "h2";

        public override bool PreMadeFilter => true;

        protected override bool InternalApply(IAuthority authority, IExchange? exchange,
            IFilteringContext? filteringContext)
        {
            return exchange?.HttpVersion == "HTTP/2";
        }
    }
}
