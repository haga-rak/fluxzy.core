using System.Collections.Generic;
using System.Net;

namespace Fluxzy.Tests.UnitTests.Equality
{
    public class FluxzyEndPointEquality : EqualityTesterBase<FluxzyEndPoint>
    {
        protected override FluxzyEndPoint Item { get; } = new("127.0.0.1", 256);

        protected override IEnumerable<FluxzyEndPoint> EqualItems { get; } =
            new FluxzyEndPoint[] {
                new (new IPEndPoint(IPAddress.Loopback, 256)),
                new ("127.0.0.1", 256)
            };

        protected override IEnumerable<FluxzyEndPoint> NotEqualItems { get; }
            = new FluxzyEndPoint[] {
                new (new IPEndPoint(IPAddress.Loopback, 2566)),
                new (new IPEndPoint(IPAddress.IPv6Loopback, 256)),
                new ("127.0.0.1", 2564),
                new ("127.0.2.1", 2564),
            };
    }
}