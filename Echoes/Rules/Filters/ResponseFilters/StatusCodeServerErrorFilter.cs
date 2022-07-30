using Echoes.Clients;

namespace Echoes.Rules.Filters.ResponseFilters
{
    public class StatusCodeServerErrorFilter : Filter
    {
        protected override bool InternalApply(IExchange exchange)
        {
            var statusCode = exchange.StatusCode;
            return statusCode is >= 500 and < 600; 
        }
        public override FilterScope FilterScope => FilterScope.ResponseHeaderReceivedFromRemote;
    }
}