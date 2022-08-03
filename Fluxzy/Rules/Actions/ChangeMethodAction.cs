// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class ChangeMethodAction : IAction
    {
        public ChangeMethodAction(string newMethod)
        {
            NewMethod = newMethod;
        }

        public string NewMethod { get; set; }

        public FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public Task Alter(ExchangeContext context, Exchange exchange, Connection connection)
        {
            exchange.Request.Header.Method = NewMethod.AsMemory();
            return Task.CompletedTask; 
        }
    }
}