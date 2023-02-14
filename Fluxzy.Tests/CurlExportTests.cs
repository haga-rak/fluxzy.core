// Copyright © 2023 Haga RAKOTOHARIVELO

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;
using Fluxzy.Tests.Cli.Scaffolding;
using Fluxzy.Tests.Common;
using Xunit;

namespace Fluxzy.Tests
{
    public class CurlExportTests
    {
        [Fact]
        public async Task Run_Cli_Output()
        {
            // Arrange 
            var protocol = "http2"; 

            var rootDir = Guid.NewGuid().ToString();
            var directoryName = $"{rootDir}/curl-test";
            var fileName = $"{rootDir}/curl-test.fxzy";

            var commandLine = "start -l 127.0.0.1/0";

            commandLine += $" -d {directoryName}";
            
            var commandLineHost = new FluxzyCommandLineHost(commandLine);

            await using (var fluxzyInstance = await commandLineHost.Run())
            {
                using var proxiedHttpClient = new ProxiedHttpClient(fluxzyInstance.ListenPort);

                var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                    $"{TestConstants.GetHost(protocol)}/global-health-check");
                
                // Act 
                using var response = await proxiedHttpClient.Client.SendAsync(requestMessage);

                await response.Content.ReadAsStringAsync();

                // Assert
                await AssertionHelper.ValidateCheck(requestMessage, null, response);
            }

            // Assert outputDirectory content

            using (IArchiveReader archiveReader = new DirectoryArchiveReader(directoryName))
            {
                var exchanges = archiveReader.ReadAllExchanges().ToList();
                var connections = archiveReader.ReadAllConnections().ToList();

                var exchange = exchanges.FirstOrDefault()!;

                var connection = connections.First();

                Assert.NotNull(exchange);
                Assert.Equal(0, await commandLineHost.ExitCode);
                Assert.Single(exchanges);
                Assert.Single(connections);

                Assert.Equal(200, exchange.StatusCode);
                Assert.Equal(connection.Id, exchange.ConnectionId);
            }

            if (Directory.Exists(directoryName))
                Directory.Delete(directoryName, true);

            if (Directory.Exists(rootDir))
                Directory.Delete(rootDir, true);

            if (File.Exists(fileName))
                File.Delete(fileName);
        }

    }
}
