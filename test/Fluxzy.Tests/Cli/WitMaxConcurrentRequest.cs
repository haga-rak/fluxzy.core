// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Tests.Cli.Scaffolding;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WitMaxConcurrentRequest : WithRuleOptionBase
    {
        [Theory]
        [InlineData(1)]
        [InlineData(8)]
        [InlineData(64)]
        public async Task WithBasicAuth(int maxConnection)
        {
            // Arrange 
            var commandLine = $"start -l 127.0.0.1/0 --max-upstream-connection={maxConnection}";

            var commandLineHost = new FluxzyCommandLineHost(commandLine);

            await using var fluxzyInstance = await commandLineHost.Run();

            using var proxiedHttpClient = new ProxiedHttpClient(fluxzyInstance.ListenPort);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, TestConstants.Http2Host);

            // Act 
            using var response = await proxiedHttpClient.Client.SendAsync(requestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
