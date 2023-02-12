// Copyright © 2023 Haga RAKOTOHARIVELO

using System;
using System.IO;
using System.Linq;
using System.Net;
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
    public class CliWebSockets
    {

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
        [InlineData(125 * 1024 + 5)]
        [InlineData(1024 * 64 * 16)]
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

    }
}
