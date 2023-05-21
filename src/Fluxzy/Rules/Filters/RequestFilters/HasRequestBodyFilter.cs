// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using Fluxzy.Misc;

namespace Fluxzy.Rules.Filters.RequestFilters
{
    /// <summary>
    ///     Select exchanges that has request body.
    /// </summary>
    [FilterMetaData(
        LongDescription = "Select request having body."
    )]
    public class HasRequestBodyFilter : Filter
    {
        public override FilterScope FilterScope => FilterScope.ResponseBodyReceivedFromRemote;

        public override string GenericName => "Has Request body";

        public override string ShortName => "req body.";

        public override bool PreMadeFilter => true;

        protected override bool InternalApply(
            IAuthority authority, IExchange? exchange,
            IFilteringContext? filteringContext)
        {
            return filteringContext?.HasRequestBody ?? false;
        }

        public override IEnumerable<FilterExample> GetExamples()
        {
            var defaultSample = GetDefaultSample();

            if (defaultSample != null)
                yield return defaultSample;
        }
    }
}
