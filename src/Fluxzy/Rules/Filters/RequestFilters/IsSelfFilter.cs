// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using Fluxzy.Clients;
using Fluxzy.Misc;

namespace Fluxzy.Rules.Filters.RequestFilters
{
    [FilterMetaData(
        LongDescription = "Check if incoming request considers fluxzy as a web server"
    )]
    public class IsSelfFilter : Filter
    {
        public override FilterScope FilterScope => FilterScope.DnsSolveDone; 

        protected override bool InternalApply(
            ExchangeContext? exchangeContext, IAuthority authority, IExchange? exchange, IFilteringContext? filteringContext)
        {
            if (exchangeContext == null || !(exchange is Exchange internalExchange) || 
                internalExchange.Connection == null)
                return false;

            if (internalExchange.Metrics.DownStreamLocalPort == exchangeContext.RemoteHostPort
                &&
                exchangeContext.RemoteHostIp != null &&
                IpUtility.LocalAddresses.Contains(exchangeContext.RemoteHostIp)) {
                return true; 
            }

            return false; 
        }

        public override IEnumerable<FilterExample> GetExamples()
        {
            yield return GetDefaultSample();

        }
    }
}
