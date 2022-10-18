// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules
{
    public class Rule
    {
        public Rule(Action action, FilterCollection filter)
        {
            if (!filter.Children.Any())
            {
                throw new ArgumentException(
                    $"You must specify at least one filter. Use {nameof(AnyFilter)} if the filter is a blank filter.",
                    nameof(filter)); 
            }

            Filter = filter;
            Action = action;

            // TODO : validate filter and action scope coherency 
            // TODO : Example response header filter should not match with DoNotDecrypt action
        }

        public Rule(Action action, params Filter [] filters)
            : this (action, new FilterCollection(filters))
        {
        }

        public FilterCollection Filter { get; set; }

        public Action Action { get; set; }

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