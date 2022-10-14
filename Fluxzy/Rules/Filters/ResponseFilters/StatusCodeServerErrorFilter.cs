namespace Fluxzy.Rules.Filters.ResponseFilters
{
    public class StatusCodeServerErrorFilter : Filter
    {
        protected override bool InternalApply(IAuthority authority, IExchange exchange)
        {
            var statusCode = exchange.StatusCode;
            return statusCode is >= 500 and < 600; 
        }
        public override FilterScope FilterScope => FilterScope.ResponseHeaderReceivedFromRemote;

        public override string FriendlyName => $"Client errors (status code is 5XX)";

        public override string GenericName => "Status code 5XX only";

        public override bool PreMadeFilter => true;
    }
}