using System.Collections.Generic;
using System.Net;
using Fluxzy.Core.Pcap.Messages;

namespace Fluxzy.Tests.UnitTests.Equality
{
    public class StoreKeyMessageEquality : EqualityTesterBase<StoreKeyMessage>
    {
        protected override StoreKeyMessage Item { get; } = 
            new(IPAddress.Loopback, 9087, 7000, "keys");

        protected override IEnumerable<StoreKeyMessage> EqualItems { get; } =
            new StoreKeyMessage[] {
                new(IPAddress.Loopback, 9087, 7000, "keys")
            };

        protected override IEnumerable<StoreKeyMessage> NotEqualItems { get; }
            = new StoreKeyMessage[] {
                new(IPAddress.IPv6Loopback, 9087, 7000, "keys"),
                new(IPAddress.Loopback, 9088, 7000, "keys"),
                new(IPAddress.Loopback, 9087, 7001, "keys"),
                new(IPAddress.Loopback, 9087, 7000, "keysoo"),
            };
    }
}