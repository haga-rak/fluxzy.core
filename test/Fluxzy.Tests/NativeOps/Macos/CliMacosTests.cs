// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using Fluxzy.NativeOps.SystemProxySetup.macOs;
using Xunit;

namespace Fluxzy.Tests.NativeOps.Macos
{
    public class CliMacosTests
    {
        private static readonly string OriginalCommandLine =
            "An asterisk (*) denotes that a network service is disabled.\r\n(1) Ethernet\r\n(Hardware Port: Ethernet, Device: en0)\r\n\r\n(2) FireWire\r\n(Hardware Port: FireWire, Device: fw0)\r\n\r\n(3) Wi-Fi\r\n(Hardware Port: Wi-Fi, Device: en1)\r\n\r\n(4) iPhone\r\n(Hardware Port: USB iPhone, Device: en4)\r\n\r\n(5) Bluetooth PAN\r\n(Hardware Port: Bluetooth PAN, Device: en3)\r\n\r\n(6) Thunderbolt Bridge\r\n(Hardware Port: Thunderbolt Bridge, Device: bridge0)\r\n";

        private readonly string _basicCommandLIneResult
            = "\r\nEnabled: yes\r\nServer: 127.0.0.1\r\nPort: 44344\r\nAuthenticated Proxy Enabled: 0";

        [Fact]
        public void TestParsingListNetworkServiceOrder()
        {
            var cmdLineResult = MacOsHelper.ParseInterfaces(OriginalCommandLine).ToList();

            Assert.Equal(6, cmdLineResult.Count);
            Assert.Contains(cmdLineResult, i => i.Name == "Ethernet");
            Assert.Contains(cmdLineResult, i => i.Name == "FireWire");
            Assert.Contains(cmdLineResult, i => i.Name == "Wi-Fi");
            Assert.Contains(cmdLineResult, i => i.Name == "iPhone");
            Assert.Contains(cmdLineResult, i => i.Name == "Bluetooth PAN");
            Assert.Contains(cmdLineResult, i => i.Name == "Thunderbolt Bridge");

            Assert.Equal(3, cmdLineResult.First(i => i.Name == "Wi-Fi").Index);
            Assert.Equal(5, cmdLineResult.First(i => i.DeviceName == "en3").Index);
        }

        [Fact]
        public void TestParsingListNetworkServiceOrderMacOsNewLine()
        {
            var commandLine = OriginalCommandLine.Replace("\r\n", "\r");

            var cmdLineResult = MacOsHelper.ParseInterfaces(commandLine).ToList();

            Assert.Equal(6, cmdLineResult.Count);
            Assert.Contains(cmdLineResult, i => i.Name == "Ethernet");
            Assert.Contains(cmdLineResult, i => i.Name == "FireWire");
            Assert.Contains(cmdLineResult, i => i.Name == "Wi-Fi");
            Assert.Contains(cmdLineResult, i => i.Name == "iPhone");
            Assert.Contains(cmdLineResult, i => i.Name == "Bluetooth PAN");
            Assert.Contains(cmdLineResult, i => i.Name == "Thunderbolt Bridge");

            Assert.Equal(3, cmdLineResult.First(i => i.Name == "Wi-Fi").Index);
            Assert.Equal(5, cmdLineResult.First(i => i.DeviceName == "en3").Index);
        }

        [Fact]
        public void TestParsingListNetworkServiceOrderUnixNewLine()
        {
            var commandLine = OriginalCommandLine.Replace("\r\n", "\n");

            var cmdLineResult = MacOsHelper.ParseInterfaces(commandLine).ToList();

            Assert.Equal(6, cmdLineResult.Count);
            Assert.Contains(cmdLineResult, i => i.Name == "Ethernet");
            Assert.Contains(cmdLineResult, i => i.Name == "FireWire");
            Assert.Contains(cmdLineResult, i => i.Name == "Wi-Fi");
            Assert.Contains(cmdLineResult, i => i.Name == "iPhone");
            Assert.Contains(cmdLineResult, i => i.Name == "Bluetooth PAN");
            Assert.Contains(cmdLineResult, i => i.Name == "Thunderbolt Bridge");

            Assert.Equal(3, cmdLineResult.First(i => i.Name == "Wi-Fi").Index);
            Assert.Equal(5, cmdLineResult.First(i => i.DeviceName == "en3").Index);
        }

        [Fact]
        public void TestParseProxySettings()
        {
            var setting =
                NetworkInterfaceProxySetting.ParseFromCommandLineResult("Enabled: No\r\nServer:\r\nPort: 0\r\nAuthenticated Proxy Enabled: 0");

            Assert.NotNull(setting);
            Assert.False(setting.Enabled);
            Assert.Equal(setting.Server, string.Empty);
            Assert.Equal(setting.Port, 0);
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
