// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Tests.Cli.Scaffolding;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WitProxyAuthentication : WithRuleOptionBase
    {
        [Theory]
        [InlineData("user", "password")]
        [InlineData("", "password")]
        [InlineData("user", "")]
        [InlineData("sdf sdf : @ https:// sd", "Bad password : // \\")]
        public async Task WithBasicAuth(string user, string password)
        {
            var proxyAuthWord = $"{WebUtility.UrlEncode(user)}:{WebUtility.UrlEncode(password)}";

            // Arrange 
            var commandLine = $"start -l 127.0.0.1/0 --proxy-auth-basic {proxyAuthWord}";

            var commandLineHost = new FluxzyCommandLineHost(commandLine);

            await using var fluxzyInstance = await commandLineHost.Run();

            using var proxiedHttpClient = new ProxiedHttpClient(fluxzyInstance.ListenPort, proxyCredential:
                new NetworkCredential(user, password));

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, TestConstants.TestDomain);

            requestMessage.Headers.Add("User-Agent", "Unit test");

            // Act 
            using var response = await proxiedHttpClient.Client.SendAsync(requestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("::")]
        [InlineData(":::")]
        [InlineData("")]
        public async Task WithBasicAuthBadArgs(string proxyAuthValue)
        {
            // Arrange 
            var commandLine = $"start -l 127.0.0.1/0 --proxy-auth-basic {proxyAuthValue}";

            var commandLineHost = new FluxzyCommandLineHost(commandLine);

            await Assert.ThrowsAsync<FluxzyBadExitCodeException>(async () => {
                await using var fluxzyInstance = await commandLineHost.Run();
            });
        }
    }
}
