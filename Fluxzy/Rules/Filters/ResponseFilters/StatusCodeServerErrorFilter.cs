﻿using System;
using Fluxzy.Misc;

namespace Fluxzy.Rules.Filters.ResponseFilters
{
    /// <summary>
    /// Select exchange that response status code indicates a server error (5XX)
    /// </summary>
    [FilterMetaData(
        LongDescription = "Select exchange that HTTP status code indicates a server/intermediary error (5XX)."
    )]
    public class StatusCodeServerErrorFilter : Filter
    {
        public override Guid Identifier => (GetType().Name + Inverted).GetMd5Guid();

        public override FilterScope FilterScope => FilterScope.ResponseHeaderReceivedFromRemote;

        public override string AutoGeneratedName => "Client errors (status code is 5XX)";

        public virtual string GenericName => "Status code 5XX only";

        public override string ShortName => "5XX";

        public override bool PreMadeFilter => true;

        protected override bool InternalApply(IAuthority authority, IExchange? exchange,
            IFilteringContext? filteringContext)
        {
            if (exchange == null)
                return false;

            var statusCode = exchange.StatusCode;

            return statusCode is >= 500 and < 600;
        }
    }
}
