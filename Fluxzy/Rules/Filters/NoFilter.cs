// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Rules.Filters
{
    public class NoFilter : Filter
    {
        public override FilterScope FilterScope => FilterScope.OnAuthorityReceived;

        public virtual string GenericName => "Block all";

        public override bool PreMadeFilter => true;

        protected override bool InternalApply(IAuthority? authority, IExchange? exchange,
            IFilteringContext? filteringContext)
        {
            return false;
        }
    }
}
