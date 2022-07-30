// Copyright © 2022 Haga Rakotoharivelo

using Echoes.Clients;

namespace Echoes.Rules.Filters.Implementations
{
    public class StatusCodeSuccess : Filter
    {
        protected override bool InternalApply(Exchange exchange)
        {
            var statusCode = exchange.Response?.Header.StatusCode ?? -1;
            return statusCode is >= 200 and < 300; 
        }
        public override FilterScope FilterScope => FilterScope.ResponseHeaderReceivedFromRemote;
    }
}