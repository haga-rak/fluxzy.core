// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Threading.Tasks;
using Fluxzy.Clients.Dns;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Dns
{
    public class DnsOverHttpsResolverTests
    {
        [Theory]
        [InlineData("CLOUDFLARE", "one.one.one.one", "1.1.1.1")]
        [InlineData("GOOGLE", "one.one.one.one", "1.1.1.1")]
        public async Task Test(string nameOfUri, string hostname, string ipAddress)
        {
            // Arrange
            var resolver = new DnsOverHttpsResolver(nameOfUri, null);
            var ip = IPAddress.Parse(ipAddress);

            // Act
            var result = await resolver.SolveDnsAll(hostname);

            // Assert
            Assert.Contains(ip, result);
        }
    }
}
