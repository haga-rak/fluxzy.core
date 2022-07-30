using Echoes.Clients;

namespace Echoes.Rules.Filters.ResponseFilters
{
    public class StatusCodeRedirectionFilter : Filter
    {
        protected override bool InternalApply(Exchange exchange)
        {
            var statusCode = exchange.Response?.Header.StatusCode ?? -1;
            return statusCode is >= 300 and < 400; 
        }
        public override FilterScope FilterScope => FilterScope.ResponseHeaderReceivedFromRemote;
    }
}