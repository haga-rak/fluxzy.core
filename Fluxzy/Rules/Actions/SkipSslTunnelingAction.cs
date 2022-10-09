// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class SkipSslTunnelingAction : IAction
    {
        public FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public Task Alter(ExchangeContext context, Exchange exchange, Connection connection)
        {
            context.BlindMode = true; 
            return Task.CompletedTask;
        }
    }
}