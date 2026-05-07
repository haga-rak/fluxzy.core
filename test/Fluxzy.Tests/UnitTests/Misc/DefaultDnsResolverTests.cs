// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fluxzy.Clients.Dns;
using Fluxzy.Core;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    public class DefaultDnsResolverTests
    {
        [Theory]
        [InlineData("fluxzy.io", "92.222.9.112")]
        [InlineData("localhost", "127.0.0.1")]
        [InlineData("badho_çà)", null)]
        public async Task Solve(string host, string ? rawIp)
        {
            var solver = new DefaultDnsResolver();

            if (rawIp != null) {

                var ip = await solver.SolveDns(host);
                Assert.Equal(IPAddress.Parse(rawIp), ip);

            }
            else {
                 var ex = await Assert.ThrowsAsync<ClientErrorException>(() => solver.SolveDns(host));
                 Assert.Equal(NetworkErrorCodes.DnsNotFound, ex.ClientError.NetworkErrorCode);
            }
        }

        [Theory]
        [InlineData("fluxzy.io", "92.222.9.112")]
        [InlineData("localhost", "127.0.0.1")]
        [InlineData("badho_çà)", null)]
        public async Task SolveDnsQuietly(string host, string ? rawIp)
        {
            var solver = new DefaultDnsResolver();

            for (int i = 0; i < 2; i++) { // to hit cache

                var ip = await solver.SolveDnsQuietly(host);

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

        [Theory]
        [InlineData("fluxzy.io", "92.222.9.112")]
        [InlineData("localhost", "127.0.0.1")]
        [InlineData("badho_çà)", null)]
        public async Task SolveMultiple(string host, string? rawIp)
        {
            var solver = new DefaultDnsResolver();

            async Task Validate()
            {
                var ip = await solver.SolveDns(host);
                Assert.Equal(IPAddress.Parse(rawIp), ip);
            }

            if (rawIp != null) {

                var tasks = new List<Task>();

                for (int i = 0; i < 10; i++) {
                    tasks.Add(Validate());
                }

                await Task.WhenAll(tasks);
            }
            else
            {
                await Assert.ThrowsAsync<ClientErrorException>(() => solver.SolveDns(host));
            }
        }

        [Theory]
        [InlineData("fluxzy.io", "92.222.9.112")]
        [InlineData("localhost", "127.0.0.1")]
        [InlineData("badho_çà)", null)]
        public async Task SolveOverHttpMultiple(string host, string? rawIp)
        {
            var solver = new DnsOverHttpsResolver("CLOUDFLARE", null);

            async Task Validate()
            {
                var ip = await solver.SolveDns(host);
                Assert.Equal(IPAddress.Parse(rawIp), ip);
            }

            if (rawIp != null) {

                var tasks = new List<Task>();

                for (int i = 0; i < 10; i++) {
                    tasks.Add(Validate());
                }

                await Task.WhenAll(tasks);
            }
            else
            {
                await Assert.ThrowsAsync<ClientErrorException>(() => solver.SolveDns(host));
            }
        }

        [Fact]
        public async Task SolveDns_Sets_DnsNoData_When_Resolver_Returns_Empty()
        {
            var solver = new EmptyResultResolver();

            var ex = await Assert.ThrowsAsync<ClientErrorException>(
                () => solver.SolveDns("probe.example"));

            Assert.Equal(NetworkErrorCodes.DnsNoData, ex.ClientError.NetworkErrorCode);
        }

        [Theory]
        [InlineData(System.Net.Sockets.SocketError.HostNotFound, NetworkErrorCodes.DnsNotFound)]
        [InlineData(System.Net.Sockets.SocketError.NoData, NetworkErrorCodes.DnsNoData)]
        [InlineData(System.Net.Sockets.SocketError.TryAgain, NetworkErrorCodes.DnsTryAgain)]
        [InlineData(System.Net.Sockets.SocketError.NoRecovery, NetworkErrorCodes.DnsFailure)]
        [InlineData(System.Net.Sockets.SocketError.AccessDenied, NetworkErrorCodes.DnsFailure)]
        public void MapDnsSocketError_Returns_Expected_Token(
            System.Net.Sockets.SocketError code, string expected)
        {
            Assert.Equal(expected, DefaultDnsResolver.MapDnsSocketError(code));
        }

        private sealed class EmptyResultResolver : DefaultDnsResolver
        {
            protected override Task<IEnumerable<IPAddress>> InternalSolveDns(string hostName)
                => Task.FromResult(Enumerable.Empty<IPAddress>());
        }
    }
}
