// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using Fluxzy.Core;

namespace Fluxzy.Tests.UnitTests.Equality
{
    public class AuthorityInfoEquality : EqualityTesterBase<AuthorityInfo>
    {
        protected override AuthorityInfo Item { get; } = new("google.com", 266, true);

        protected override IEnumerable<AuthorityInfo> EqualItems { get; } =
            new[] {
                new AuthorityInfo("google.com", 266, true),
                new AuthorityInfo(new Authority("google.com", 266, true))
            };

        protected override IEnumerable<AuthorityInfo> NotEqualItems { get; }
            = new[] {
                new AuthorityInfo("google.coma", 266, true),
                new AuthorityInfo("google.com", 2661, true),
                new AuthorityInfo("google.com", 266, false)
            };
    }
}
