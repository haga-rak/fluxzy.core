// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Rules.Filters;

public class NoFilter : Filter
{
    protected override bool InternalApply(IAuthority authority, IExchange exchange)
    {
        return false; 
    }

    public override FilterScope FilterScope => FilterScope.OnAuthorityReceived;
}