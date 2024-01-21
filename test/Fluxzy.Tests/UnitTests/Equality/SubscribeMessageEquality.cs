using System.Collections.Generic;
using System.Net;
using Fluxzy.Core.Pcap.Messages;

namespace Fluxzy.Tests.UnitTests.Equality
{
    public class SubscribeMessageEquality : EqualityTesterBase<SubscribeMessage>
    {
        protected override SubscribeMessage Item { get; } = 
            new(IPAddress.Loopback, 9087, 7000, "keys");

        protected override IEnumerable<SubscribeMessage> EqualItems { get; } =
            new SubscribeMessage[] {
                new(IPAddress.Loopback, 9087, 7000, "keys")
            };

        protected override IEnumerable<SubscribeMessage> NotEqualItems { get; }
            = new SubscribeMessage[] {
                new(IPAddress.IPv6Loopback, 9087, 7000, "keys"),
                new(IPAddress.Loopback, 9088, 7000, "keys"),
                new(IPAddress.Loopback, 9087, 7001, "keys"),
                new(IPAddress.Loopback, 9087, 7000, "keysoo"),
            };
    }
}