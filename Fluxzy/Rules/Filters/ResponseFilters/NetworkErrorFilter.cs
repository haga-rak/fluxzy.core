﻿// // Copyright 2022 - Haga Rakotoharivelo
// 

using System;
using Fluxzy.Misc;

namespace Fluxzy.Rules.Filters.ResponseFilters
{
    [FilterMetaData(
        LongDescription = "Select exchange that fails due to network error."
    )]
    public class NetworkErrorFilter : Filter
    {
        public override Guid Identifier => (GetType().Name + Inverted).GetMd5Guid();
        
        public override FilterScope FilterScope => FilterScope.ResponseHeaderReceivedFromRemote;

        public override string AutoGeneratedName => "Network error";

        public virtual string GenericName => "Network error (528)";

        public override string ShortName => "neterr.";
        
        protected override bool InternalApply(IAuthority authority, IExchange? exchange, IFilteringContext? filteringContext)
        {
            if (exchange == null)
                return false;

            return exchange.StatusCode == 528; 

        }
    }
}