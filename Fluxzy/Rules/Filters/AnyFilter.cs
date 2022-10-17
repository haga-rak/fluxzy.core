// Copyright © 2022 Haga Rakotoharivelo

using System;

namespace Fluxzy.Rules.Filters
{
    public class AnyFilter : Filter
    {
        protected override bool InternalApply(IAuthority authority, IExchange exchange)
        {
            return true; 
        }

        public override Guid Identifier { get; init; } = Guid.Parse("A62052B4-516D-492E-93B3-2888CDA4E92D");

        public override FilterScope FilterScope => FilterScope.OnAuthorityReceived;

        public override string GenericName => "Any";

        public override string ShortName => "any";

        public override bool PreMadeFilter => true;

        public override string? Description { get; set; } = "No filter";

        public static AnyFilter Default { get;  } = new()
        {
            Locked = true
        };
    }


}