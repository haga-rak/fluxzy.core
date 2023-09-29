// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using Fluxzy.Misc;

namespace Fluxzy.Rules.Filters.ResponseFilters
{
    /// <summary>
    ///     Select exchanges that has css content as response body.
    /// </summary>
    [FilterMetaData(
        LongDescription = "Select exchanges having response content type matching a font payload."
    )]
    public class  FontFilter : ResponseHeaderFilter
    {
        public FontFilter()
            : base("font", StringSelectorOperation.Contains, "Content-Type")
        {
            
        }

        public override string AutoGeneratedName { get; } = "Font files only";

        public override Guid Identifier => (GetType().Name + Inverted).GetMd5Guid();

        public override string GenericName => "All font files";

        public override string ShortName => "font";

        public override bool PreMadeFilter => true;

        public override IEnumerable<FilterExample> GetExamples()
        {
            var defaultSample = GetDefaultSample();

            if (defaultSample != null)
                yield return defaultSample;
        }
    }
}