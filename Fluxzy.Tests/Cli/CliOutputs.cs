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
        public async Task Run_Cli_Output(string proto, CaptureType rawCap, bool @out, bool rule)
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

            if (proto.EndsWith("-bc")) {
                commandLine += " --bouncy-castle"; 
            }

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
                using var proxiedHttpClient = new ProxiedHttpClient(fluxzyInstance.ListenPort);

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

        public static IEnumerable<object[]> GetSingleRequestParametersNoDecrypt
        {
            get
            {
                var protocols = new[] { "http11", "http2", "http11-bc", "http2-bc", "plainhttp11" };
                var withPcapStatus = new[] { CaptureType.None, CaptureType.Pcap, CaptureType.PcapOutOfProc };
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

    public enum CaptureType
    {
        None, 
        Pcap,
        PcapOutOfProc
    }
}
