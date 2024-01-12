// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Misc.IpUtils;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Util
{
    public class NetworkInterfaceInfoProviderTests
    {
        [Fact]
        public void GetNetworkInterfaceInfos()
        {
            var interfaces = NetworkInterfaceInfoProvider.GetNetworkInterfaceInfos();

            foreach (var @interface in interfaces) {
                Assert.NotNull(@interface.IPAddress);
                Assert.NotNull(@interface.InterfaceName);
            }
        }
    }
}
