// Copyright © 2022 Haga Rakotoharivelo

using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests.Tools;
using Fluxzy.Tests.Utils;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class SimpleCaptureTests
    {
        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]
        public async Task Run_Single_Request_And_Halt_Fluxzy(string host)
        {
            var commandLine = "start -l 127.0.0.1/0";
            var commandLineHost = new FluxzyCommandLineHost(commandLine);

            await using var fluxzyInstance = await commandLineHost.Run();

            var proxiedHttpClient = new ProxiedHttpClient(fluxzyInstance.ListenPort);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post,
                $"{host}/global-health-check");

            await using var randomStream = new RandomDataStream(48, 23632, true);
            await using var hashedStream = new HashedStream(randomStream);

            requestMessage.Content = new StreamContent(hashedStream);
            requestMessage.Headers.Add("X-Test-Header-256", "That value");

            using var response = await proxiedHttpClient.Client.SendAsync(requestMessage);

            await AssertionHelper.ValidateCheck(requestMessage, hashedStream.Hash, response);
        }
    }
}