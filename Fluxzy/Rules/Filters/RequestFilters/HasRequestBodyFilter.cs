// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Misc;
using System;

namespace Fluxzy.Rules.Filters.RequestFilters
{
    public class HasRequestBodyFilter : Filter
    {
        protected override bool InternalApply(IAuthority authority, IExchange exchange, IFilteringContext? filteringContext)
        {
            return filteringContext?.HasRequestBody ?? false;
        }

        public override Guid Identifier => $"{GetType().Name}{Inverted}".GetMd5Guid();

        public override FilterScope FilterScope => FilterScope.ResponseBodyReceivedFromRemote;

        public override string GenericName => "Has Request body";

        public override string ShortName => "req body.";

        public override bool PreMadeFilter => true;

    }
}