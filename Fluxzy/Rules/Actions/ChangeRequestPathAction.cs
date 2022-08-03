// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions;

public class ChangeRequestPathAction : IAction
{
    public ChangeRequestPathAction(string newPath)
    {
        NewPath = newPath;
    }

    public string NewPath { get; set; }

    public FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

    public Task Alter(ExchangeContext context, Exchange exchange, Connection connection)
    {
        exchange.Request.Header.Path = NewPath.AsMemory();
        return Task.CompletedTask; 
    }
}