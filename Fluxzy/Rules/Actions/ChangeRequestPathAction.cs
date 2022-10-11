// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions
{
    public class ChangeRequestPathAction : Action
    {
        public ChangeRequestPathAction(string newPath)
        {
            NewPath = newPath;
        }

        public string NewPath { get; set; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override Task Alter(ExchangeContext context, Exchange exchange, Connection connection)
        {
            exchange.Request.Header.Path = NewPath.AsMemory();
            return Task.CompletedTask; 
        }
    }
}