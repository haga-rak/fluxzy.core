namespace Fluxzy.Rules.Filters.ResponseFilters
{
    public class StatusCodeRedirectionFilter : Filter
    {
        protected override bool InternalApply(IAuthority authority, IExchange exchange)
        {
            var statusCode = exchange.StatusCode;
            return statusCode is >= 300 and < 400; 
        }
        public override FilterScope FilterScope => FilterScope.ResponseHeaderReceivedFromRemote;

        public override string FriendlyName => $"Status code is redirection (3XX)";

        public override string GenericName => "Status code 3XX only";

        public override bool PreMadeFilter => true;
    }
}