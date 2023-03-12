// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class BreakPointAction : Action
    {
        public override FilterScope ActionScope => FilterScope.OnAuthorityReceived;

        public override string DefaultDescription { get; } = "Breakpoint";

        public override ValueTask Alter(ExchangeContext context, Exchange? exchange, Connection? connection)
        {
            if (exchange == null || exchange.Id == 0 || context.BreakPointManager == null)
                return default;

            context.BreakPointContext = context.BreakPointManager.GetOrCreate(exchange.Id); 



        }
    }

}
