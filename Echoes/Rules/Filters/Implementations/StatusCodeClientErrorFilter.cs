using Echoes.Clients;

namespace Echoes.Rules.Filters.Implementations;

public class StatusCodeClientErrorFilter : Filter
{
    protected override bool InternalApply(Exchange exchange)
    {
        var statusCode = exchange.Response?.Header.StatusCode ?? -1;
        return statusCode is >= 400 and < 500; 
    }
}