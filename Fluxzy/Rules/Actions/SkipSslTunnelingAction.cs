// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class SkipSslTunnelingAction : Action
    {
        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public override Task Alter(ExchangeContext context, Exchange exchange, Connection connection)
        {
            context.BlindMode = true; 
            return Task.CompletedTask;
        }
    }
}