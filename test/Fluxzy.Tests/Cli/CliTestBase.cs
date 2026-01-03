// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Tests.Cli.Scaffolding;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class CliTestBase
    {
        protected async Task Run_Cli_Output(string proto, CaptureType rawCap, 
            bool @out, bool rule, bool useSock5 = false)
        {
            // Arrange 

            var rootDir = $"ab0{Guid.NewGuid()}";

            var directoryName = $"{rootDir}/{proto}-{rawCap}-{@out}-{rule}";
            var fileName = $"{rootDir}/{proto}-{rawCap}-{@out}-{rule}.fxzy";

            var commandLine = "start -l 127.0.0.1/0";

            commandLine += @out ? $" -d {directoryName}" : $" -o {fileName}";

            if (rawCap != CaptureType.None)
                commandLine += " -c";

            if (rawCap == CaptureType.PcapOutOfProc)
                commandLine += "  --external-capture";

            if (proto.EndsWith("-bc"))
                commandLine += " --bouncy-castle";

            if (rule)
            {
                Directory.CreateDirectory(rootDir);

                var ruleFile = $"{rootDir}/rules.yml";

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

            await using (var fluxzyInstance = await commandLineHost.Run(30))
            {
                using var proxiedHttpClient = new ProxiedHttpClient(fluxzyInstance.ListenPort,
                    useSock5: useSock5);

                var requestMessage = new HttpRequestMessage(HttpMethod.Post,
                    $"{TestConstants.GetHost(proto)}/global-health-check");

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
            }

            using (IArchiveReader archiveReader = @out
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

                if (rawCap != CaptureType.None)
                {
                    var rawCapStream = archiveReader.GetRawCaptureStream(connection.Id);
                    Assert.True(await rawCapStream!.DrainAsync(disposeStream: true) > 0);
                }

                if (rule)
                {
                    var alterHeader =
                        exchange.GetRequestHeaders().FirstOrDefault(t => t.Name.ToString() == "x-fluxzy");

                    Assert.Contains("x-fluxzy", exchange.GetRequestHeaders().Select(t => t.Name.ToString()));
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

    }
}
