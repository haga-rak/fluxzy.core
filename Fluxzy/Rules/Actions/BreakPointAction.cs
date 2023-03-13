// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class BreakPointAction : Action
    {
        public override FilterScope ActionScope => FilterScope.OutOfScope;

        public override string DefaultDescription { get; } = "Breakpoint";

        public override ValueTask Alter(
            ExchangeContext context, Exchange? exchange, Connection? connection, FilterScope scope)
        {
            if (exchange == null || exchange.Id == 0 || context.BreakPointManager == null)
                return default;

            if (context.BreakPointContext == null)
                context.BreakPointContext = context.BreakPointManager.GetOrCreate(exchange, scope);

            return default; 

        }
    }

}
