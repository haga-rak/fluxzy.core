// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using Fluxzy.Core;
using Fluxzy.Misc;

namespace Fluxzy.Rules.Filters.ResponseFilters
{
    /// <summary>
    ///     Select exchanges that http status code indicates a redirect (3XX)
    /// </summary>
    [FilterMetaData(
        LongDescription = "Select exchanges that HTTP status code indicates a redirect (3XX)."
    )]
    public class StatusCodeRedirectionFilter : Filter
    {
        public override Guid Identifier => (GetType().Name + Inverted).GetMd5Guid();

        public override FilterScope FilterScope => FilterScope.ResponseHeaderReceivedFromRemote;

        public override string AutoGeneratedName => "Status code is redirection (3XX)";

        public override string ShortName => "3XX";

        public override bool PreMadeFilter => true;

        protected override bool InternalApply(
            ExchangeContext? exchangeContext, IAuthority authority, IExchange? exchange,
            IFilteringContext? filteringContext)
        {
            if (exchange == null)
                return false;

            var statusCode = exchange.StatusCode;

            return statusCode is >= 300 and < 400;
        }

        public override IEnumerable<FilterExample> GetExamples()
        {
            var defaultSample = GetDefaultSample();

            if (defaultSample != null)
                yield return defaultSample;
        }
    }
}
