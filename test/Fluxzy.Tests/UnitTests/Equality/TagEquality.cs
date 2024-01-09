using System;
using System.Collections.Generic;

namespace Fluxzy.Tests.UnitTests.Equality
{
    public class TagEquality : EqualityTesterBase<Tag>
    {
        protected override Tag Item { get; } = new(new Guid("EA196F45-6709-4E1D-AFB9-1EFA8A61CBD1"), "tag");

        protected override IEnumerable<Tag> EqualItems { get; } =
            new Tag[] {
                new(new Guid("EA196F45-6709-4E1D-AFB9-1EFA8A61CBD1"), "tag")
            };

        protected override IEnumerable<Tag> NotEqualItems { get; }
            = new Tag[] {
                new(new Guid("EA196F45-6709-4E1D-AFB9-1EFA8A61CBD2"), "tag"),
                new(new Guid("EA196F45-6709-4E1D-AFB9-1EFA8A61CBD1"), "tago"),
                new("tag"),
            };
    }
}