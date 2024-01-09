using System.Collections.Generic;
using Fluxzy.Core;

namespace Fluxzy.Tests.UnitTests.Equality
{
    public class AuthorityEquality : EqualityTesterBase<Authority>
    {
        protected override Authority Item { get; } = new Authority("host", 256, true);

        protected override IEnumerable<Authority> EqualItems { get; } =
            new[] { new Authority("host", 256, true) };

        protected override IEnumerable<Authority> NotEqualItems { get; }
            = new[] {
                new Authority("host", 256, false),
                new Authority("hosta", 256, true),
                new Authority("host", 2856, true),
            };
    }
}