using System.Collections.Generic;
using System.Net;

namespace Fluxzy.Tests.UnitTests.Equality
{
    public class ProxyBindPointEquality : EqualityTesterBase<ProxyBindPoint>
    {
        protected override ProxyBindPoint Item { get; } = new(new IPEndPoint(IPAddress.Loopback, 256), true);

        protected override IEnumerable<ProxyBindPoint> EqualItems { get; } =
            new ProxyBindPoint[] {
                new (new IPEndPoint(IPAddress.Loopback, 256), true),
            };

        protected override IEnumerable<ProxyBindPoint> NotEqualItems { get; }
            = new ProxyBindPoint[] {
                new (new IPEndPoint(IPAddress.IPv6Loopback, 256), true),
                new (new IPEndPoint(IPAddress.Loopback, 256), false),
                new (new IPEndPoint(IPAddress.Loopback, 251), true),
            };
    }
}