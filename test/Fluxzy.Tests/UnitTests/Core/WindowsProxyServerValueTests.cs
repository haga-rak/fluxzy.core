// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Utils.NativeOps.SystemProxySetup.Win;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Core
{
    public class WindowsProxyServerValueTests
    {
        [Fact]
        public void Build_Writes_Http_And_Https_Only()
        {
            Assert.Equal("http=127.0.0.1:44344;https=127.0.0.1:44344",
                WindowsProxyHelper.BuildProxyServerValue("127.0.0.1", 44344));
        }

        [Theory]
        [InlineData("127.0.0.1:44344", "127.0.0.1", 44344, false)]
        [InlineData("http=127.0.0.1:44344;https=127.0.0.1:44344", "127.0.0.1", 44344, true)]
        [InlineData("HTTP=proxy:3128;HTTPS=proxy:3128", "proxy", 3128, true)]
        [InlineData("http=proxy:3128;https=proxy:3129", "proxy", 3128, false)]
        [InlineData("http=proxy:3128;https=proxy:3128;ftp=proxy:21", "proxy", 3128, false)]
        [InlineData("http=proxy:3128", "proxy", 3128, false)]
        [InlineData("https=proxy:3128", "proxy", 3128, false)]
        public void Parse_Reads_Host_And_Port(string raw, string host, int port, bool lossless)
        {
            var result = WindowsProxyHelper.TryParseProxyServer(raw, out var actualHost, out var actualPort);

            Assert.Equal(lossless, result);
            Assert.Equal(host, actualHost);
            Assert.Equal(port, actualPort);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("proxy")]
        [InlineData("proxy:")]
        [InlineData("socks=proxy:1080")]
        [InlineData("http=proxy")]
        public void Parse_Rejects_Unusable_Values(string raw)
        {
            var result = WindowsProxyHelper.TryParseProxyServer(raw, out var host, out var port);

            Assert.False(result);
            Assert.Equal(ProxyConstants.NoProxyWord, host);
            Assert.Equal(-1, port);
        }

        [Fact]
        public void Build_Then_Parse_Round_Trips()
        {
            var raw = WindowsProxyHelper.BuildProxyServerValue("127.0.0.1", 8080);

            Assert.True(WindowsProxyHelper.TryParseProxyServer(raw, out var host, out var port));
            Assert.Equal("127.0.0.1", host);
            Assert.Equal(8080, port);
        }
    }
}
