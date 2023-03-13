// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;
using YamlDotNet.Serialization;

namespace Fluxzy.Rules
{
    public class Rule
    {
        public Rule(Action action, Filter filter)
        {
            Filter = filter;
            Action = action;
        }

        [YamlIgnore]
        public Guid Identifier { get; set; } = Guid.NewGuid();

        public string? Name { get; set; }

        public Filter Filter { get; set; }

        public Action Action { get; set; }

        public int Order { get; set; }

        [YamlIgnore]
        public bool InScope => Filter.FilterScope <= Action.ActionScope;

        public ValueTask Enforce(
            ExchangeContext context,
            Exchange? exchange,
            Connection? connection, FilterScope filterScope)
        {
            // TODO put a decent filtering context here 
            if (Filter.Apply(context.Authority, exchange, null))
                return Action.Alter(context, exchange, connection, filterScope);

            return default;
        }

        public override string ToString()
        {
            return $"Action : {Action.FriendlyName} / Filter : {Filter.FriendlyName}";
        }
    }
}
