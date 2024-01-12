// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Tests.UnitTests.Equality
{
    public class SearchStreamResultEquality : EqualityTesterBase<SearchStreamResult>
    {
        protected override SearchStreamResult Item { get; } = new SearchStreamResult(9);

        protected override IEnumerable<SearchStreamResult> EqualItems { get; } =
            new[] { new SearchStreamResult(9) };

        protected override IEnumerable<SearchStreamResult> NotEqualItems { get; }
            = new[] {
                new SearchStreamResult(4)
            };
    }
}
