// Copyright © 2022 Haga Rakotoharivelo

using System;
using Fluxzy.Misc;

namespace Fluxzy.Rules.Filters
{
    public class AnyFilter : Filter
    {
        public override Guid Identifier => $"{Inverted}A62052B4-516D-492E-93B3-2888CDA4E92D".GetMd5Guid();

        public override FilterScope FilterScope => FilterScope.OnAuthorityReceived;

        public virtual string GenericName => "Any";

        public override string ShortName => "any";

        public override bool PreMadeFilter => true;

        public override string? Description { get; set; } = "Any requests";

        public static AnyFilter Default { get; } = new()
        {
            Locked = true
        };

        protected override bool InternalApply(IAuthority? authority, IExchange? exchange,
            IFilteringContext? filteringContext)
        {
            return true;
        }
    }
}
