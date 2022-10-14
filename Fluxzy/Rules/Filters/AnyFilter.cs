// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Rules.Filters
{
    public class AnyFilter : Filter
    {
        protected override bool InternalApply(IAuthority authority, IExchange exchange)
        {
            return true; 
        }

        public override FilterScope FilterScope => FilterScope.OnAuthorityReceived;

        public override string GenericName => "Any";

        public override bool PreMadeFilter => true;
    }
}