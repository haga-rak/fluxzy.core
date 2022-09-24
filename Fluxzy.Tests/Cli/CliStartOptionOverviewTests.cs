// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests.Cli.Scaffolding;
using Fluxzy.Tests.Tools;
using Fluxzy.Tests.Utils;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class CliStartOptionOverviewTests
    {
        public static IEnumerable<object[]> GetSingleRequestParameters
        {
            get
            {
                var allHosts = new[] { TestConstants.Http11Host, TestConstants.Http2Host };
                var decryptionStatus = new[] { false, true };

                foreach (var host in allHosts)
                foreach (var decryptStat in decryptionStatus)
                    yield return new object[] { host, decryptStat };
            }
        }

        [Theory]
        [MemberData(nameof(GetSingleRequestParameters))]
        public async Task Run_Single_Request_And_Halt_Fluxzy(string host, bool noDecryption)
        {
            // Arrange 
            var commandLine = "start -l 127.0.0.1/0";

            if (noDecryption)
                commandLine += " -ss";

            var commandLineHost = new FluxzyCommandLineHost(commandLine);

            await using var fluxzyInstance = await commandLineHost.Run();
            using var proxiedHttpClient = new ProxiedHttpClient(fluxzyInstance.ListenPort);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{host}/global-health-check");

            await using var randomStream = new RandomDataStream(48, 23632, true);
            await using var hashedStream = new HashedStream(randomStream);

            requestMessage.Content = new StreamContent(hashedStream);
            requestMessage.Headers.Add("X-Test-Header-256", "That value");

            // Act 
            using var response = await proxiedHttpClient.Client.SendAsync(requestMessage);

            // Assert
            await AssertionHelper.ValidateCheck(requestMessage, hashedStream.Hash, response);
        }
    }
}