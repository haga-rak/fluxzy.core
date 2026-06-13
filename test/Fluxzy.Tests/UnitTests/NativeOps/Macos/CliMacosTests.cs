// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using Fluxzy.Utils.NativeOps.SystemProxySetup.macOs;
using Xunit;

namespace Fluxzy.Tests.UnitTests.NativeOps.Macos
{
    public class CliMacosTests
    {
        private readonly string _basicCommandLIneResult
            = "\r\nEnabled: yes\r\nServer: 127.0.0.1\r\nPort: 44344\r\nAuthenticated Proxy Enabled: 0";

        [Fact]
        public void TestParseProxySettings()
        {
            var setting =
                NetworkInterfaceProxySetting.ParseFromCommandLineResult("Enabled: No\r\nServer:\r\nPort: 0\r\nAuthenticated Proxy Enabled: 0");

            Assert.NotNull(setting);
            Assert.False(setting.Enabled);
            Assert.Equal(string.Empty, setting.Server);
            Assert.Equal(0, setting.Port);
        }

        [Fact]
        public void TestParseProxySettingsRegularValue()
        {
            var setting = NetworkInterfaceProxySetting.ParseFromCommandLineResult(_basicCommandLIneResult);

            Assert.NotNull(setting);
            Assert.True(setting.Enabled);
            Assert.Equal("127.0.0.1", setting.Server);
            Assert.Equal(44344, setting.Port);
        }

        [Fact]
        public void TestParseProxySettingsRegularValueMacosStyle()
        {
            var setting = NetworkInterfaceProxySetting.ParseFromCommandLineResult(_basicCommandLIneResult.Replace("\r\n", "\r"));

            Assert.NotNull(setting);
            Assert.True(setting.Enabled);
            Assert.Equal("127.0.0.1", setting.Server);
            Assert.Equal(44344, setting.Port);
        }

        [Fact]
        public void TestParseProxySettingsLimitCase()
        {
            var setting = NetworkInterfaceProxySetting.ParseFromCommandLineResult(
                "\r\nEnabled: yes\r\nEnabled: yes\r\nServer: 127.0.0.1\r\nPort: 44344\r\nAuthenticated Proxy Enabled: 0");

            Assert.Null(setting);
        }

        private const string ServiceOrderResult =
            "An asterisk (*) denotes that a network service is disabled.\n" +
            "(1) USB 10/100/1000 LAN\n" +
            "(Hardware Port: USB 10/100/1000 LAN, Device: en7)\n" +
            "\n" +
            "(2) Wi-Fi\n" +
            "(Hardware Port: Wi-Fi, Device: en0)\n";

        [Fact]
        public void TestParseNetworkServices()
        {
            var services = NetworkInterface.ParseNetworkServices(
                ServiceOrderResult.Split('\n'));

            Assert.Equal(2, services.Count);

            var wifi = services.Single(s => s.DeviceName == "en0");

            // networksetup is addressed by service name, not by BSD device name.
            Assert.Equal("Wi-Fi", wifi.ServiceName);
            Assert.Equal("Wi-Fi", wifi.HardwarePort);
        }

        [Fact]
        public void TestParseNetworkServicesKeepsRenamedServiceName()
        {
            // A user can rename a service, so the service name no longer matches the hardware port.
            var services = NetworkInterface.ParseNetworkServices(new[] {
                "(1) Home Wi-Fi",
                "(Hardware Port: Wi-Fi, Device: en0)"
            });

            var iface = Assert.Single(services);

            Assert.Equal("en0", iface.DeviceName);
            Assert.Equal("Home Wi-Fi", iface.ServiceName);
            Assert.Equal("Wi-Fi", iface.HardwarePort);
        }

        [Fact]
        public void TestParseByPassDomainsEmpty()
        {
            var domains = MacOsHelper.ParseByPassDomains("There aren't any bypass domains set on Wi-Fi.");

            Assert.Empty(domains);
        }

        [Fact]
        public void TestParseByPassDomains()
        {
            var domains = MacOsHelper.ParseByPassDomains("*.local\r\n169.254/16\r\nexample.com");

            Assert.Equal(new[] { "*.local", "169.254/16", "example.com" }, domains);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(",, ,")]
        public void TestParseServiceListEmptyIsNull(string? raw)
        {
            Assert.Null(MacOsProxyConfiguration.ParseList(raw));
        }

        [Fact]
        public void TestParseServiceListTrimsAndDropsEmpty()
        {
            var list = MacOsProxyConfiguration.ParseList(" Wi-Fi , Ethernet ,, USB 10/100/1000 LAN ");

            Assert.Equal(new[] { "Wi-Fi", "Ethernet", "USB 10/100/1000 LAN" }, list);
        }
    }
}
