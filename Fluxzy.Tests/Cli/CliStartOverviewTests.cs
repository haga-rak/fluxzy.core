// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;
using Fluxzy.Tests.Cli.Scaffolding;
using Fluxzy.Tests.Tools;
using Fluxzy.Tests.Utils;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class CliStartOverviewTests
    {
        public static IEnumerable<object[]> GetSingleRequestParameters
        {
            get
            {
                var protocols = new[] { "http11", "http2" };
                var decryptionStatus = new[] { false, true };

                foreach (var protocol in protocols)
                foreach (var decryptStat in decryptionStatus)
                    yield return new object[] { protocol, decryptStat };
            }
        }
        public static IEnumerable<object[]> GetSingleRequestParametersNoDecrypt
        {
            get
            {
                var protocols = new[] { "http11", "http2" };
                var withPcapStatus = new[] { false, true };
                var directoryParams = new[] { false, true };


                foreach (var protocol in protocols)
                foreach (var withPcap in withPcapStatus)
                foreach (var directoryParam in directoryParams)
                    yield return new object[] { protocol, withPcap, directoryParam };
            }
        }

        [Theory]
        [MemberData(nameof(GetSingleRequestParameters))]
        public async Task Run_Cli(string protocol, bool noDecryption)
        {
            // Arrange 
            var commandLine = "start -l 127.0.0.1/0";

            if (noDecryption)
                commandLine += " -ss";

            var commandLineHost = new FluxzyCommandLineHost(commandLine);

            await using var fluxzyInstance = await commandLineHost.Run();
            using var proxiedHttpClient = new ProxiedHttpClient(fluxzyInstance.ListenPort);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{TestConstants.GetHost(protocol)}/global-health-check");

            await using var randomStream = new RandomDataStream(48, 23632, true);
            await using var hashedStream = new HashedStream(randomStream);

            requestMessage.Content = new StreamContent(hashedStream);
            requestMessage.Headers.Add("X-Test-Header-256", "That value");

            // Act 
            using var response = await proxiedHttpClient.Client.SendAsync(requestMessage);

            // Assert
            await AssertionHelper.ValidateCheck(requestMessage, hashedStream.Hash, response);
            
        }


        [Theory]
        [MemberData(nameof(GetSingleRequestParametersNoDecrypt))]
        public async Task Run_Cli_Output(string protocol, bool withPcap, bool outputDirectory)
        {
            // Arrange 

            var directoryName = $"{Guid.NewGuid()}/{protocol}-{withPcap}-{outputDirectory}";
            var fileName = $"{Guid.NewGuid()}/{protocol}-{withPcap}-{outputDirectory}.fxzy";

            var commandLine = $"start -l 127.0.0.1/0";

            commandLine += outputDirectory ? $" -d {directoryName}" : $" -o {fileName}";

            if (withPcap)
            {
                commandLine += " -c"; 
            }

            try
            {
                var commandLineHost = new FluxzyCommandLineHost(commandLine);
                var requestBodyLength = 23632;
                var bodyLength = 0L; 

                await using (var fluxzyInstance = await commandLineHost.Run())
                {
                    using var proxiedHttpClient = new ProxiedHttpClient(fluxzyInstance.ListenPort);

                    var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{TestConstants.GetHost(protocol)}/global-health-check");

                    await using var randomStream = new RandomDataStream(48, requestBodyLength, true);
                    await using var hashedStream = new HashedStream(randomStream);

                    requestMessage.Content = new StreamContent(hashedStream);
                    requestMessage.Headers.Add("X-Test-Header-256", "That value");

                    // Act 
                    using var response = await proxiedHttpClient.Client.SendAsync(requestMessage);


                    bodyLength = response.Content.Headers.ContentLength ?? -1;
                    // Assert
                    await AssertionHelper.ValidateCheck(requestMessage, hashedStream.Hash, response);
                }

                // Assert outputDirectory content

                using (IArchiveReader archiveReader = outputDirectory
                           ? new DirectoryArchiveReader(directoryName)
                           : new FluxzyArchiveReader(fileName))
                {

                    var exchanges = archiveReader.ReadAllExchanges().ToList();
                    var connections = archiveReader.ReadAllConnections().ToList();

                    var exchange = exchanges.First();
                    var connection = connections.First();

                    Assert.Equal(0, await commandLineHost.ExitCode);
                    Assert.Single(exchanges);
                    Assert.Single(connections);

                    Assert.Equal(200, exchange.StatusCode);
                    Assert.Equal(connection.Id, exchange.ConnectionId);
                    Assert.Equal(requestBodyLength, await archiveReader.GetRequestBody(exchange.Id).Drain(disposeStream: true));
                    Assert.Equal(bodyLength, await archiveReader.GetResponseBody(exchange.Id).Drain(disposeStream: true));

                    Assert.Contains(exchange.RequestHeader.Headers,
                        t => t.Name.Span.Equals("X-Test-Header-256".AsSpan(), StringComparison.Ordinal));

                    if (withPcap)
                    {
                        Assert.True(await archiveReader.GetRawCaptureStream(connection.Id).Drain(disposeStream: true) > 0);
                    }
                }

                if (Directory.Exists(directoryName))
                    Directory.Delete(directoryName, true);

                if (File.Exists(fileName))
                    File.Delete(fileName);

            }
            finally
            {
            }

        }
    }
}