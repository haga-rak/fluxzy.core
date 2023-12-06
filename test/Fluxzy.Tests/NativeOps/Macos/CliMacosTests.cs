// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Threading;
using Fluxzy.Utils.NativeOps.SystemProxySetup.macOs;
using Xunit;

namespace Fluxzy.Tests.NativeOps.Macos
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
    }
}
