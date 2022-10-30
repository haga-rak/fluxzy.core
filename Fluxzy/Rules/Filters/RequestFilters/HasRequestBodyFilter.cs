// Copyright © 2022 Haga Rakotoharivelo

using System;
using Fluxzy.Misc;

namespace Fluxzy.Rules.Filters.RequestFilters
{
    public class HasRequestBodyFilter : Filter
    {
        public override Guid Identifier => $"{GetType().Name}{Inverted}".GetMd5Guid();

        public override FilterScope FilterScope => FilterScope.ResponseBodyReceivedFromRemote;

        public virtual string GenericName => "Has Request body";

        public override string ShortName => "req body.";

        public override bool PreMadeFilter => true;

        protected override bool InternalApply(IAuthority? authority, IExchange? exchange,
            IFilteringContext? filteringContext)
        {
            return filteringContext?.HasRequestBody ?? false;
        }
    }
}
