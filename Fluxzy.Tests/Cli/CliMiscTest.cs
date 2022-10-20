// Copyright © 2022 Haga Rakotoharivelo

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests.Cli.Scaffolding;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class Cli_Misc
    {

        [Fact]
        public async Task Run_Cli_Wait_For_Complete_When_304()
        {
            // Arrange 
            var commandLine = "start -l 127.0.0.1/0";

            var commandLineHost = new FluxzyCommandLineHost(commandLine);

            await using var fluxzyInstance = await commandLineHost.Run();
            using var proxiedHttpClient = new ProxiedHttpClient(fluxzyInstance.ListenPort);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post,
                "https://registry.2befficient.io:40300/status/304");

            requestMessage.Headers.Add("xxxx", new string('a', 1024 * 2));

            var response = await proxiedHttpClient.Client.SendAsync(requestMessage);
            var content = await response.Content.ReadAsStringAsync(); 


            Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        }
    }
}