// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;

namespace Fluxzy.Tests.UnitTests.Equality
{
    public class AgentEquality : EqualityTesterBase<Agent>
    {
        protected override Agent Item { get; } = new(9, "browser");

        protected override IEnumerable<Agent> EqualItems { get; } =
            new[] {
                new Agent(9, "browser")
            };

        protected override IEnumerable<Agent> NotEqualItems { get; }
            = new[] {
                new Agent(91, "browser"),
                new Agent(9, "browsers")
            };
    }
}
