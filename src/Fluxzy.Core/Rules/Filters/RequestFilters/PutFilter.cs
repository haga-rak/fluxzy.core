// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;

namespace Fluxzy.Rules.Filters.RequestFilters
{
    public class PutFilter : MethodFilter
    {
        public PutFilter()
            : base("PUT")
        {
        }

        public override string GenericName => "PUT only";

        public override string ShortName => "put";

        public override bool PreMadeFilter => true;

        public override IEnumerable<FilterExample> GetExamples()
        {
            var defaultSample = GetDefaultSample();

            if (defaultSample != null)
                yield return defaultSample;
        }
    }
}
