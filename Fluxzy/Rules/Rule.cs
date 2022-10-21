// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules
{
    public class Rule
    {
        public Rule(Action action, Filter filter)
        {
            Filter = filter;
            Action = action;
        }

        public Guid Identifier { get; set; } = Guid.NewGuid(); 

        public Filter Filter { get; set; }

        public Action Action { get; set; }

        public bool InScope => Filter.FilterScope <= Action.ActionScope;

        public ValueTask Enforce(ExchangeContext context,
            Exchange exchange,
            Connection connection)
        {
            if (Filter.Apply(context.Authority, exchange))
            {
                return Action.Alter(context, exchange, connection);
            }

            return default;
        }
    }
}