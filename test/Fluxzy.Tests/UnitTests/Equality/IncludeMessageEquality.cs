using System.Collections.Generic;
using System.Net;
using Fluxzy.Core.Pcap.Messages;

namespace Fluxzy.Tests.UnitTests.Equality
{
    public class IncludeMessageEquality : EqualityTesterBase<IncludeMessage>
    {
        protected override IncludeMessage Item { get; } = 
            new(IPAddress.Loopback, 9087);

        protected override IEnumerable<IncludeMessage> EqualItems { get; } =
            new IncludeMessage[] {
                new(IPAddress.Loopback, 9087)
            };

        protected override IEnumerable<IncludeMessage> NotEqualItems { get; }
            = new IncludeMessage[] {
                new(IPAddress.IPv6Loopback, 9087),
                new(IPAddress.Loopback, 9088),
            };
    }
}