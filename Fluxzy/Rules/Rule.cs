// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules
{
    public class Rule
    {
        public Rule(IAction action, FilterCollection filter)
        {
            Filter = filter;
            Action = action;

            // TODO : validate filter and action scope coherency 
            // TODO : Example response header filter should not match with DoNotDecrypt action
        }

        public Rule(IAction action, params Filter [] filters)
            : this (action, new FilterCollection(filters))
        {
        }

        public FilterCollection Filter { get; set; }

        public IAction Action { get; set; }

        public async Task Enforce(ExchangeContext context,
            Exchange exchange,
            Connection connection)
        {
            if (Filter.Apply(context.Authority, exchange))
            {
                await Action.Alter(context, exchange, connection);
            }
        }
    }
}