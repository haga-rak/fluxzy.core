// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using Fluxzy.Clients;

namespace Fluxzy.Rules.Filters
{
    /// <summary>
    ///     Select nothing
    /// </summary>
    public class NoFilter : Filter
    {
        public override FilterScope FilterScope => FilterScope.OnAuthorityReceived;

        public override string GenericName => "Block all";

        public override bool PreMadeFilter => true;

        protected override bool InternalApply(
            ExchangeContext? exchangeContext, IAuthority authority, IExchange? exchange,
            IFilteringContext? filteringContext)
        {
            return false;
        }

        public override IEnumerable<FilterExample> GetExamples()
        {
            var defaultSample = GetDefaultSample();

            if (defaultSample != null)
                yield return defaultSample;
        }
    }
}
