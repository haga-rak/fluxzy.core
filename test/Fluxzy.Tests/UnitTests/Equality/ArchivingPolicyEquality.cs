// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;

namespace Fluxzy.Tests.UnitTests.Equality
{
    public class ArchivingPolicyEquality : EqualityTesterBase<ArchivingPolicy>
    {
        protected override ArchivingPolicy Item { get; } = ArchivingPolicy.CreateFromDirectory("yes");

        protected override IEnumerable<ArchivingPolicy> EqualItems { get; } =
            new[] {
                ArchivingPolicy.CreateFromDirectory("yes")
            };

        protected override IEnumerable<ArchivingPolicy> NotEqualItems { get; }
            = new[] {
                ArchivingPolicy.CreateFromDirectory("yeso"),
                ArchivingPolicy.None,
            };
    }
}
