// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class BreakPointAction : Action
    {
        [JsonIgnore]
        public ExchangeContext? ExchangeContext { get; private set; }

        public override FilterScope ActionScope => FilterScope.OutOfScope;

        public override string DefaultDescription { get; } = "Breakpoint";

        public override ValueTask Alter(
            ExchangeContext context,
            Exchange? exchange,
            Connection? connection,
            FilterScope scope,
            BreakPointManager breakPointManager)
        {
            if (exchange == null || exchange.Id == 0)
                return default;

            if (context.BreakPointContext == null) {
                ExchangeContext = context;

                context.BreakPointContext = breakPointManager.GetOrCreate(exchange,
                    AnyFilter.Default, // Propage filter until action
                    scope);
            }

            return default;
        }
    }
}
