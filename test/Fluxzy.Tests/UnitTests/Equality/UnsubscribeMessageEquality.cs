using System.Collections.Generic;
using Fluxzy.Core.Pcap.Messages;

namespace Fluxzy.Tests.UnitTests.Equality
{
    public class UnsubscribeMessageEquality : EqualityTesterBase<UnsubscribeMessage>
    {
        protected override UnsubscribeMessage Item { get; } = new(3);

        protected override IEnumerable<UnsubscribeMessage> EqualItems { get; } =
            new UnsubscribeMessage[] {
                new(3)
            };

        protected override IEnumerable<UnsubscribeMessage> NotEqualItems { get; }
            = new UnsubscribeMessage[] {
                new(4),
            };
    }
}