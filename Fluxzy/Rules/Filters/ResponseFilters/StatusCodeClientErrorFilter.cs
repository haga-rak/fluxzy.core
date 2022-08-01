namespace Fluxzy.Rules.Filters.ResponseFilters
{
    public class StatusCodeClientErrorFilter : Filter
    {
        protected override bool InternalApply(IExchange exchange)
        {
            var statusCode = exchange.StatusCode;
            return statusCode is >= 400 and < 500; 
        }

        public override FilterScope FilterScope => FilterScope.ResponseHeaderReceivedFromRemote;

        public override string FriendlyName => $"Client errors (status code is 4XX)";
    }
}