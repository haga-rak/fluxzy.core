using Echoes.Clients;

namespace Echoes.Rules.Filters.Implementations
{
    public class StatusCodeServerErrorFilter : Filter
    {
        protected override bool InternalApply(Exchange exchange)
        {
            var statusCode = exchange.Response?.Header.StatusCode ?? -1;
            return statusCode is >= 500 and < 600; 
        }
        public override FilterScope FilterScope => FilterScope.ResponseHeaderReceivedFromRemote;
    }
}