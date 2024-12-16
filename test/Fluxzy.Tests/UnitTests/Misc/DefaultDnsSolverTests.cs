// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Threading.Tasks;
using Fluxzy.Clients.Dns;
using Fluxzy.Core;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    public class DefaultDnsSolverTests
    {
        [Theory]
        [InlineData("fluxzy.io", "162.19.47.110")]
        [InlineData("localhost", "127.0.0.1")]
        //[InlineData("no.domain.here.yes.fluxzy.io", null)]
        [InlineData("badho_çà)", null)]
        public async Task Solve(string host, string ? rawIp)
        {
            var solver = new DefaultDnsSolver();

            if (rawIp != null) {

                var ip = await solver.SolveDns(host, null); 
                Assert.Equal(IPAddress.Parse(rawIp), ip);

            }
            else {
                 await Assert.ThrowsAsync<ClientErrorException>(() => solver.SolveDns(host, null));
            }
        }

        [Theory]
        [InlineData("fluxzy.io", "162.19.47.110")]
        [InlineData("localhost", "127.0.0.1")]
        // [InlineData("no.domain.here.yes.fluxzy.io", null)]
        [InlineData("badho_çà)", null)]
        public async Task SolveDnsQuietly(string host, string ? rawIp)
        {
            var solver = new DefaultDnsSolver();

            for (int i = 0; i < 2; i++) { // to hit cache

                var ip = await solver.SolveDnsQuietly(host, null);

                if (rawIp != null)
                {

                    Assert.Equal(IPAddress.Parse(rawIp), ip);
                }
                else
                {
                    Assert.Null(ip);
                }
            }
        }
    }
}
