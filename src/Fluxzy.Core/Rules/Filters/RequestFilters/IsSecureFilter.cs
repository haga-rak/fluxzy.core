// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using Fluxzy.Core;

namespace Fluxzy.Rules.Filters.RequestFilters
{
    /// <summary>
    /// Select secure exchange only (non plain HTTP).
    /// </summary>
    [FilterMetaData(
        LongDescription = "Select secure exchange only (non plain HTTP)."
    )]
    public class IsSecureFilter : Filter
    {
        public override FilterScope FilterScope => FilterScope.OnAuthorityReceived;

        public override string GenericName => "Secure only";

        public override string AutoGeneratedName { get; } = "Secure only";

        public override string ShortName => "sec";

        public override bool PreMadeFilter => true;

        protected override bool InternalApply(
            ExchangeContext? exchangeContext, IAuthority authority, IExchange? exchange,
            IFilteringContext? filteringContext)
        {
            if (exchangeContext != null)
                return exchangeContext.Secure;

            return authority?.Secure ?? false;
        }

        public override IEnumerable<FilterExample> GetExamples()
        {
            var defaultSample = GetDefaultSample();

            if (defaultSample != null)
                yield return defaultSample;
        }
    }
}
