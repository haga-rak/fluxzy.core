// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class ChangeRequestMethodAction : Action
    {
        public ChangeRequestMethodAction(string newMethod)
        {
            NewMethod = newMethod;
        }

        public string NewMethod { get; set; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override ValueTask Alter(ExchangeContext context, Exchange exchange, Connection connection)
        {
            exchange.Request.Header.Method = NewMethod.AsMemory();
            return default;
        }
    }
}