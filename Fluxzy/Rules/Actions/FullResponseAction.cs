// Copyright © 2022 Haga Rakotoharivelo

using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.Mock;
using Fluxzy.Rules.Filters;

namespace Fluxzy.Rules.Actions;

public class FullResponseAction : IAction
{
    public FullResponseAction(PreMadeResponse preMadeResponse)
    {
        PreMadeResponse = preMadeResponse;
    }

    public PreMadeResponse PreMadeResponse { get; set; }

    public FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient; 

    public Task Alter(ExchangeContext context, Exchange exchange, Connection connection)
    {
        context.PreMadeResponse = PreMadeResponse;
        return Task.CompletedTask; 
    }
}