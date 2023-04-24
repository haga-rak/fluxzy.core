// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Misc;

namespace Fluxzy.Rules.Filters.ResponseFilters
{
    /// <summary>
    ///     Select exchange that has css content as response body.
    /// </summary>
    [FilterMetaData(
        LongDescription = "Select exchange having response content type mime matching css."
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
    }
}