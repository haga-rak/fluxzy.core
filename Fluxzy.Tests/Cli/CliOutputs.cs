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

namespace Fluxzy.Tests.Cli
{
    public class CliOutputs
    {
        [Theory]
        [MemberData(nameof(GetSingleRequestParametersNoDecrypt))]
        public async Task Run_Cli_Output(string protocol,
            bool withPcap, bool outputDirectory, bool withSimpleRule)
        {
            // Arrange 

            var rootDir = "ab0" + Guid.NewGuid();
           // var rootDir = "d:\\aaa-test";
            var directoryName = $"{rootDir}/{protocol}-{withPcap}-{outputDirectory}-{withSimpleRule}";
            var fileName = $"{rootDir}/{protocol}-{withPcap}-{outputDirectory}-{withSimpleRule}.fxzy";

            var commandLine = "start -l 127.0.0.1/0 --external-capture";

            commandLine += outputDirectory ? $" -d {directoryName}" : $" -o {fileName}";

            if (withPcap)
                commandLine += " -c";

            if (withSimpleRule)
            {
                var ruleFile = $"rules.yml";

                var yamlContent = """
                rules:
                  - filter: 
                      typeKind: AnyFilter        
                    action : 
                      typeKind: AddRequestHeaderAction
                      headerName: x-fluxzy
                      headerValue: on
                """;

                File.WriteAllText(ruleFile, yamlContent);
                commandLine += $" -r {ruleFile}";
            }

            var commandLineHost = new FluxzyCommandLineHost(commandLine);
            var requestBodyLength = 23632;
            var bodyLength = 0L;

            await using (var fluxzyInstance = await commandLineHost.Run())
            {
                using var proxiedHttpClient = new ProxiedHttpClient(fluxzyInstance.ListenPort);

                var requestMessage = new HttpRequestMessage(HttpMethod.Post,
                    $"{TestConstants.GetHost(protocol)}/global-health-check");

                await using var randomStream = new RandomDataStream(48, requestBodyLength, true);
                await using var hashedStream = new HashedStream(randomStream);

                requestMessage.Content = new StreamContent(hashedStream);
                requestMessage.Headers.Add("X-Test-Header-256", "That value");

                // Act 
                using var response = await proxiedHttpClient.Client.SendAsync(requestMessage);

                bodyLength = response.Content.Headers.ContentLength ?? -1;

                var res = await response.Content.ReadAsStringAsync();

                // Assert
                await AssertionHelper.ValidateCheck(requestMessage, hashedStream.Hash, response);

                //await Task.Delay(1000);
            }

            // Assert outputDirectory content

            using (IArchiveReader archiveReader = outputDirectory
                       ? new DirectoryArchiveReader(directoryName)
                       : new FluxzyArchiveReader(fileName))
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

                Assert.Equal(requestBodyLength,
                    await archiveReader.GetRequestBody(exchange.Id)!.DrainAsync(disposeStream: true));

                Assert.Equal(bodyLength,
                    await archiveReader.GetResponseBody(exchange.Id)!.DrainAsync(disposeStream: true));

                Assert.Contains(exchange.RequestHeader.Headers,
                    t => t.Name.Span.Equals("X-Test-Header-256".AsSpan(), StringComparison.Ordinal));

                if (withPcap)
                {
                    try
                    {
                        var rawCapStream = archiveReader.GetRawCaptureStream(connection.Id);
                        
                        Assert.True(await rawCapStream!.DrainAsync(disposeStream: true) > 0);

                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"{directoryName} found", ex);
                    }
                }

                if (withSimpleRule)
                {
                    var alterHeader =
                        exchange.GetRequestHeaders().FirstOrDefault(t => t.Name.ToString() == "x-fluxzy");

                    Assert.NotNull(alterHeader);
                    Assert.Equal("on", alterHeader!.Value.ToString());
                }
            }

            if (Directory.Exists(directoryName))
                Directory.Delete(directoryName, true);

            if (Directory.Exists(rootDir))
                Directory.Delete(rootDir, true);

            if (File.Exists(fileName))
                File.Delete(fileName);
        }



        public static IEnumerable<object[]> GetSingleRequestParametersNoDecrypt
        {
            get
            {
                var protocols = new[] { "http11", "http2", "plainhttp11" };
                var withPcapStatus = new[] { false, true };
                var directoryParams = new[] { false, true };
                var withSimpleRules = new[] { false, true };

                foreach (var protocol in protocols)
                foreach (var withPcap in withPcapStatus)
                foreach (var directoryParam in directoryParams)
                foreach (var withSimpleRule in withSimpleRules)
                    yield return new object[] { protocol, withPcap, directoryParam, withSimpleRule };
            }
        }

    }
}
