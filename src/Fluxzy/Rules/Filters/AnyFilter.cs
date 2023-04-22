// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Misc;

namespace Fluxzy.Rules.Filters
{
    /// <summary>
    ///     Select all request
    /// </summary>
    [FilterMetaData(
        LongDescription = "Select all exchanges",
        DoNotHistorize = true,
        ToolBarFilter = true,
        ToolBarFilterOrder = 0
    )]
    public class AnyFilter : Filter
    {
        public override Guid Identifier => $"{Inverted}A62052B4-516D-492E-93B3-2888CDA4E92D".GetMd5Guid();

        public override FilterScope FilterScope => FilterScope.OnAuthorityReceived;

        public override string GenericName => "Any";

        public override string ShortName => "any";

        public override bool PreMadeFilter => true;

        public override string? Description { get; set; } = "Any requests";

        public static AnyFilter Default { get; } = new() {
            Locked = true
        };

        protected override bool InternalApply(
            IAuthority authority, IExchange? exchange,
            IFilteringContext? filteringContext)
        {
            return true;
        }
    }
}
