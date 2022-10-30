﻿using System;
using Fluxzy.Misc;

namespace Fluxzy.Rules.Filters.ResponseFilters
{
    public class StatusCodeRedirectionFilter : Filter
    {
        public override Guid Identifier => (GetType().Name + Inverted).GetMd5Guid();

        public override FilterScope FilterScope => FilterScope.ResponseHeaderReceivedFromRemote;

        public override string AutoGeneratedName => "Status code is redirection (3XX)";

        public virtual string GenericName => "Status code 3XX only";

        public override string ShortName => "3XX";

        public override bool PreMadeFilter => true;

        protected override bool InternalApply(IAuthority? authority, IExchange? exchange,
            IFilteringContext? filteringContext)
        {
            if (exchange == null)
                return false;

            var statusCode = exchange.StatusCode;

            return statusCode is >= 300 and < 400;
        }
    }
}
