﻿// // Copyright 2022 - Haga Rakotoharivelo
// 

namespace Fluxzy.Rules.Filters
{
    public class HasCommentFilter : Filter
    {
        public override FilterScope FilterScope => FilterScope.OutOfScope;

        public override string AutoGeneratedName => "Has any comment";

        public override bool PreMadeFilter => true;

        protected override bool InternalApply(IAuthority authority, IExchange? exchange,
            IFilteringContext? filteringContext)
        {
            return !string.IsNullOrWhiteSpace(exchange?.Comment);
        }
    }
}
