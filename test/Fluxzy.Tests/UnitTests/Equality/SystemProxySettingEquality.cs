using System.Collections.Generic;
using Fluxzy.Core.Proxy;

namespace Fluxzy.Tests.UnitTests.Equality
{
    public class SystemProxySettingEquality : EqualityTesterBase<SystemProxySetting>
    {
        protected override SystemProxySetting Item { get; } = new("127.0.0.1", 256, "google.com");

        protected override IEnumerable<SystemProxySetting> EqualItems { get; } =
            new SystemProxySetting[] {
                new("127.0.0.1", 256, "google.com")
            };

        protected override IEnumerable<SystemProxySetting> NotEqualItems { get; }
            = new SystemProxySetting[] {
                new("192.0.0.1", 256, "google.com"),
                new("127.0.0.1", 257, "google.com"),
                new("127.0.0.1", 256, "googled.com"),
                new("127.0.0.1", 256, ""),
            };
    }
}