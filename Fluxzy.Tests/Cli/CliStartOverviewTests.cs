// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H11;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;
using Fluxzy.Tests.Cli.Scaffolding;
using Fluxzy.Tests.Common;
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
                var withSimpleRules = new[] { false, true };

                foreach (var protocol in protocols)
                foreach (var withPcap in withPcapStatus)
                foreach (var directoryParam in directoryParams)
                foreach (var withSimpleRule in withSimpleRules)
                    yield return new object[] { protocol, withPcap, directoryParam, withSimpleRule };
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

            var requestMessage =
                new HttpRequestMessage(HttpMethod.Post, $"{TestConstants.GetHost(protocol)}/global-health-check");

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
        public async Task Run_Cli_Output(string protocol, bool withPcap, bool outputDirectory, bool withSimpleRule)
        {
            // Arrange 

            var directoryName = $"{Guid.NewGuid()}/{protocol}-{withPcap}-{outputDirectory}";
            var fileName = $"{Guid.NewGuid()}/{protocol}-{withPcap}-{outputDirectory}.fxzy";

            var commandLine = "start -l 127.0.0.1/0";

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

                await response.Content.ReadAsStringAsync();

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
                    Assert.True(await archiveReader.GetRawCaptureStream(connection.Id)!.DrainAsync(disposeStream: true) > 0);

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

            if (File.Exists(fileName))
                File.Delete(fileName);
        }

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

            //  requestMessage.Headers.Add("xxxx", new string('a', 1024 * 2));

            var response = await proxiedHttpClient.Client.SendAsync(requestMessage);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        }

        [Fact]
        public async Task Run_Cli_For_Web_Socket_Tests()
        {
            // Arrange 
            var directoryName = "ws";

            var commandLine = $"start -l 127.0.0.1/0 -d {directoryName}";

            var originalMessage = "AZERTY123!%$";

            var messageBytes = Encoding.UTF8.GetBytes(originalMessage);

            var commandLineHost = new FluxzyCommandLineHost(commandLine);

            await using (var fluxzyInstance = await commandLineHost.Run())
            {
                using var ws = new ClientWebSocket
                {
                    Options = { Proxy = new WebProxy($"http://127.0.0.1:{fluxzyInstance.ListenPort}") }
                };

                var uri = new Uri($"{TestConstants.WssHost}/websocket");

                var buffer = (Memory<byte>)new byte[4096];

                await ws.ConnectAsync(uri, CancellationToken.None);
                await ws.ReceiveAsync(buffer, CancellationToken.None);

                await ws.SendAsync(messageBytes, WebSocketMessageType.Text,
                    WebSocketMessageFlags.EndOfMessage | WebSocketMessageFlags.DisableCompression,
                    CancellationToken.None);

                var res = await ws.ReceiveAsync(buffer, CancellationToken.None);

                var resultHash = Encoding.ASCII.GetString(buffer.Slice(0, res.Count).Span);
                var expectedHash = Convert.ToBase64String(SHA1.HashData(messageBytes));

                Assert.Equal(expectedHash, resultHash);
            }

            using (IArchiveReader archiveReader = new DirectoryArchiveReader(directoryName))
            {
                var exchanges = archiveReader.ReadAllExchanges().ToList();
                var connections = archiveReader.ReadAllConnections().ToList();

                var exchange = exchanges.First();
                var connection = connections.First();

                var fistSentMessage =
                    exchange.WebSocketMessages!.First(m => m.Direction == WsMessageDirection.Sent);

                var expectedMessage = Encoding.UTF8.GetString(fistSentMessage.Data!);

                Assert.Equal(0, await commandLineHost.ExitCode);
                Assert.Single(exchanges);
                Assert.Single(connections);

                Assert.Equal(101, exchange.StatusCode);
                Assert.Equal(connection.Id, exchange.ConnectionId);
                Assert.Equal(expectedMessage, originalMessage);
            }

            if (Directory.Exists(directoryName))
                Directory.Delete(directoryName, true);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(1024 * 64 * 16 )]
        [InlineData(125 * 1024 + 5)]
        public async Task Run_Cli_For_Web_Socket_Req_Res(int length)
        {
            var random = new Random(9);

            // Arrange 
            var directoryName = $"ws_{length}";

            if (Directory.Exists(directoryName))
                Directory.Delete(directoryName, true);

            var commandLine = $"start -l 127.0.0.1/0 -d {directoryName}";

            var originalMessage = RandomString(length, random);

            var messageBytes = Encoding.UTF8.GetBytes(originalMessage);

            var commandLineHost = new FluxzyCommandLineHost(commandLine);

            await using (var fluxzyInstance = await commandLineHost.Run())
            {
                using var ws = new ClientWebSocket
                {
                    Options = { Proxy = new WebProxy($"http://127.0.0.1:{fluxzyInstance.ListenPort}") }
                };

                var uri = new Uri($"{TestConstants.WssHost}/websocket-req-res");

                var buffer = (Memory<byte>)new byte[4096];

                await ws.ConnectAsync(uri, CancellationToken.None);

                await ws.SendAsync(messageBytes, WebSocketMessageType.Text,
                    WebSocketMessageFlags.EndOfMessage | WebSocketMessageFlags.DisableCompression,
                    CancellationToken.None);

                var res = await ws.ReceiveAsync(buffer, CancellationToken.None);

                var resultHash = Encoding.ASCII.GetString(buffer.Slice(0, res.Count).Span);
                var expectedHash = Convert.ToBase64String(SHA1.HashData(messageBytes));

                Assert.Equal(expectedHash, resultHash);

                await Task.Delay(200);
            }

            using (IArchiveReader archiveReader = new DirectoryArchiveReader(directoryName))
            {
                var exchanges = archiveReader.ReadAllExchanges().ToList();
                var connections = archiveReader.ReadAllConnections().ToList();

                var exchange = exchanges.First();
                var connection = connections.First();

                var fistSentMessage =
                    exchange.WebSocketMessages!.First(m => m.Direction == WsMessageDirection.Sent);

                var testData = fistSentMessage.Data;

                if (testData == null)
                {
                    using var fileStream = archiveReader.GetRequestWebsocketContent(exchange.Id, fistSentMessage.Id);
                    testData = fileStream.ToArrayGreedy();
                }

                var resultMessage = Encoding.UTF8.GetString(testData);

                Assert.Equal(0, await commandLineHost.ExitCode);
                Assert.Single(exchanges);
                Assert.Single(connections);

                Assert.Equal(101, exchange.StatusCode);
                Assert.Equal(fistSentMessage.WrittenLength, fistSentMessage.Length);

                Assert.Equal(connection.Id, exchange.ConnectionId);

                Assert.Equal(originalMessage, resultMessage);
                Assert.Equal(originalMessage.Length, resultMessage.Length);
            }

            if (Directory.Exists(directoryName))
                Directory.Delete(directoryName, true);
        }

        public static string RandomString(int length, Random random)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789/123456789";

            return new string(Enumerable.Repeat(chars, length)
                                        .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        //[Fact]
        public async Task Run_Cli_Aggressive_Request_Response()
        {
            // Arrange 
            var commandLine = "start -l 127.0.0.1/0";

            var commandLineHost = new FluxzyCommandLineHost(commandLine);

            await using var fluxzyInstance = await commandLineHost.Run();

            var bodyLength = 307978;
            var count = 700;

            var url =
                "https://cci-news.com/wp-content/mmr/e44b2584-1639397252.min.js";

            for (var i = 0; i < 2; i++)
                await Task.WhenAll(
                    Enumerable.Range(0, count).Select((i, e) =>
                        AggressiveCallProducer.MakeAggressiveCall(
                            $"{url}",
                            fluxzyInstance.ListenPort, bodyLength, i % 2 == 0))
                );
            // await Task.Delay(30 * 1000);
        }
    }

    public static class AggressiveCallProducer
    {
        public static async Task MakeAggressiveCall(string url, int listenPort, int bodyLength, bool abort)
        {
            using var handler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://127.0.0.1:{listenPort}")
            };

            var cancellationTokenSource = new CancellationTokenSource();

            using var client = new HttpClient(handler);

            var responseMessageTask = client.GetAsync(url, cancellationTokenSource.Token);
            var contentLength = -1;

            if (abort)
                // await Task.Delay(50);
                cancellationTokenSource.Cancel();

            try
            {
                var responseMessage = await responseMessageTask;

                contentLength = (await responseMessage.Content.ReadAsStreamAsync(cancellationTokenSource.Token))
                    .Drain();
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!abort)
                Assert.Equal(bodyLength, contentLength);
        }
    }
}
