// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    public class UpstreamProxyManagerTests
    {
        [Theory]
        [InlineData("example.com", 443, "Basic yes")]
        [InlineData("example.com", 443, null)]
        public void WriteConnectHeader(string host, int port, string ? authorizationHeader)
        {
            // Arrange
            var config = new ConnectConfiguration(host, port, authorizationHeader);

            var buffer = new byte[1024];

            // Act
            var totalWritten = UpstreamProxyManager.WriteConnectHeader(buffer, config);

            var finalString = Encoding.ASCII.GetString(buffer, 0, totalWritten);

            var expected = $"CONNECT {host}:{port} HTTP/1.1\r\n" +
                           $"Host: {host}:{port}\r\n" +
                           (config.ProxyAuthorizationHeader == null ? string.Empty : $"Proxy-Authorization: {config.ProxyAuthorizationHeader}\r\n") +
                           $"Connection: keep-alive\r\n\r\n";

            // Assert
            Assert.Equal(expected, finalString);
        }
        
        [Theory]
        [InlineData("HTTP/1.1 200 Connection established\r\n\r\n",
            UpstreamProxyConnectResult.Ok)]
        [InlineData("HTTP/1.1 401 Unauthorized\r\n\r\n",
            UpstreamProxyConnectResult.InvalidStatusCode)]
        [InlineData("HTTP/1.1 407 Proxy Authentication Required\r\n\r\n",
            UpstreamProxyConnectResult.AuthenticationRequired)]
        [InlineData("HTTP/1.1 401 Unauthorized\r\n\r\nTrailing",
            UpstreamProxyConnectResult.InvalidResponse)]
        [InlineData("HTTP/1.1 abc Unauthorized\r\n\r\n",
            UpstreamProxyConnectResult.InvalidResponse)]
        [InlineData("Hd",
            UpstreamProxyConnectResult.InvalidResponse)]
        [InlineData("",
            UpstreamProxyConnectResult.InvalidResponse)]
        [InlineData("\r\n\r\n",
            UpstreamProxyConnectResult.InvalidResponse)]
        public async Task ValidateConnect(string proxyResponse, UpstreamProxyConnectResult expectedResult)
        {
            // Arrange
            var config = new ConnectConfiguration("example.com", 443);
            var rawResponse = Encoding.ASCII.GetBytes(proxyResponse);

            var buffer = new byte[1024];

            var totalWritten = UpstreamProxyManager.WriteConnectHeader(buffer, config);
            var inStream = new MemoryStream(buffer, 0, totalWritten);
            var outStream = new MemoryStream(rawResponse);

            // Act
            var result = await UpstreamProxyManager.Connect(config, inStream, outStream);

            // Assert

            Assert.Equal(expectedResult, result);
        }
    }
}
