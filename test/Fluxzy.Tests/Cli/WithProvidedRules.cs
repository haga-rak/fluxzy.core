// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;
using Fluxzy.Tests._Files;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Tests.Cli.Scaffolding;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithProvidedRules
    {
        public static IEnumerable<object[]> GetSingleRequestParameters {
            get
            {
                var protocols = new[] { "http11", "http2" };
                var decryptionStatus = new[] { false, true };

                foreach (var protocol in protocols)
                foreach (var decryptStat in decryptionStatus) {
                    yield return new object[] { protocol, decryptStat };
                }
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
        [InlineData(false)]
        [InlineData(true)]
        public async Task Run_Cli_With_ClientCertificate(bool forceH11)
        {
            // Arrange 
            var commandLine = "start -l 127.0.0.1/0";
            var ruleFile = "rules.yml";

            File.WriteAllBytes("cc.pfx", StorageContext.client_cert);

            var yamlContent = """
                rules:
                  - filter: 
                      typeKind: AnyFilter        
                    action : 
                      typeKind: SetClientCertificateAction
                      clientCertificate: 
                        pkcs12File: cc.pfx
                        pkcs12Password: Multipass85/
                        retrieveMode: FromPkcs12
                """;

            var yamlContentForceHttp11 = """
                rules:
                  - filter: 
                      typeKind: AnyFilter        
                    action : 
                      typeKind: SetClientCertificateAction
                      clientCertificate: 
                        pkcs12File: cc.pfx
                        pkcs12Password: Multipass85/
                        retrieveMode: FromPkcs12 
                  - filter: 
                      typeKind: AnyFilter        
                    action : 
                      typeKind: ForceHttp11Action
                """;

            File.WriteAllText(ruleFile, forceH11 ? yamlContentForceHttp11 : yamlContent);

            commandLine += $" -r {ruleFile}";

            var commandLineHost = new FluxzyCommandLineHost(commandLine);

            await using var fluxzyInstance = await commandLineHost.Run();
            using var proxiedHttpClient = new ProxiedHttpClient(fluxzyInstance.ListenPort);

            var requestMessage =
                new HttpRequestMessage(HttpMethod.Get, $"{TestConstants.GetHost("http2")}/certificate");

            requestMessage.Headers.Add("X-Test-Header-256", "That value");

            // Act 
            using var response = await proxiedHttpClient.Client.SendAsync(requestMessage);

            var thumbPrint = await response.Content.ReadAsStringAsync();
            var expectedThumbPrint = "960b00317d47d0d52d04a3a03b045e96bf3be3a3";

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedThumbPrint, thumbPrint, StringComparer.OrdinalIgnoreCase);
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

            var response = await proxiedHttpClient.Client.SendAsync(requestMessage);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
        }

        [Theory]
        [InlineData(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/107.0.0.0 Safari/537.36",
            "Chrome 107")]
        [InlineData("Bad User Agent", "Other")]
        public async Task Run_Cli_And_Validate_User_Agent(string userAgent, string expectedFriendlyName)
        {
            // Arrange 
            var directory = nameof(Run_Cli_And_Validate_User_Agent);
            var commandLine = $"start -l 127.0.0.1/0 -d {directory} --parse-ua";

            await using (var fluxzyInstance = await FluxzyCommandLineHost.CreateAndRun(commandLine)) {
                using var proxiedHttpClient = new ProxiedHttpClient(fluxzyInstance.ListenPort);

                var requestMessage = new HttpRequestMessage(HttpMethod.Post,
                    "https://registry.2befficient.io:40300/status/200");

                requestMessage.Headers.Add("User-Agent", userAgent);

                var response = await proxiedHttpClient.Client.SendAsync(requestMessage);
                await response.Content.ReadAsStringAsync();

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            using (IArchiveReader archiveReader = new DirectoryArchiveReader(directory)) {
                var exchanges = archiveReader.ReadAllExchanges().ToList();
                archiveReader.ReadAllConnections().ToList();

                var exchange = exchanges.FirstOrDefault()!;

                Assert.NotNull(exchange);
                Assert.NotNull(exchange.Agent);
                Assert.StartsWith(expectedFriendlyName, exchange.Agent!.FriendlyName);
            }

            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        [Fact]
        public async Task Run_Cli_And_Validate_User_Absence()
        {
            // Arrange 
            var directory = nameof(Run_Cli_And_Validate_User_Absence);
            var commandLine = $"start -l 127.0.0.1/0 -d {directory}";

            await using (var fluxzyInstance = await FluxzyCommandLineHost.CreateAndRun(commandLine)) {
                using var proxiedHttpClient = new ProxiedHttpClient(fluxzyInstance.ListenPort);

                var requestMessage = new HttpRequestMessage(HttpMethod.Post,
                    "https://registry.2befficient.io:40300/status/200");

                requestMessage.Headers.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/107.0.0.0 Safari/537.36");

                var response = await proxiedHttpClient.Client.SendAsync(requestMessage);
                await response.Content.ReadAsStringAsync();

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            using (IArchiveReader archiveReader = new DirectoryArchiveReader(directory)) {
                var exchanges = archiveReader.ReadAllExchanges().ToList();
                archiveReader.ReadAllConnections().ToList();

                var exchange = exchanges.FirstOrDefault()!;

                Assert.NotNull(exchange);
                Assert.Null(exchange.Agent);
            }

            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

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

            for (var i = 0; i < 2; i++) {
                await Task.WhenAll(
                    Enumerable.Range(0, count).Select((i, e) =>
                        AggressiveCallProducer.MakeAggressiveCall(
                            $"{url}",
                            fluxzyInstance.ListenPort, bodyLength, i % 2 == 0))
                );
            }

            // await Task.Delay(30 * 1000);
        }

        [Fact]
        public async Task Run_Post_Plain_Http()
        {
            // Arrange 
            var directory = nameof(Run_Post_Plain_Http);
            var commandLine = $"start -l 127.0.0.1/0 -d {directory}";

            await using (var fluxzyInstance = await FluxzyCommandLineHost.CreateAndRun(commandLine)) {
                using var proxiedHttpClient = new ProxiedHttpClient(fluxzyInstance.ListenPort);

                var requestMessage =
                    new HttpRequestMessage(HttpMethod.Post,
                        $"{TestConstants.GetHost("plainhttp11")}/global-health-check");

                requestMessage.Content = new ByteArrayContent(Encoding.UTF8.GetBytes("ABCD"));

                var response = await proxiedHttpClient.Client.SendAsync(requestMessage);
                await response.Content.ReadAsStringAsync();

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            using (IArchiveReader archiveReader = new DirectoryArchiveReader(directory)) {
                var exchanges = archiveReader.ReadAllExchanges().ToList();
                archiveReader.ReadAllConnections().ToList();

                var exchange = exchanges.FirstOrDefault()!;

                Assert.NotNull(exchange);
                Assert.Null(exchange.Agent);
            }

            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        [Theory]
        [InlineData("http11")]
        [InlineData("http2")]
        [InlineData("plainhttp11")]
        public async Task Run_Cli_Chunked_Request_Body(string protocol)
        {
            // Arrange 
            var commandLine = "start -l 127.0.0.1/0";

            var commandLineHost = new FluxzyCommandLineHost(commandLine);

            var cookieValue =
                "THz3tkJCYR8vOQwpxS556BSjlkj/i9SBwNtof+R1Oyjkr4bznOKd0m/7EkYpjl+03rKFCfxdJcqTE8i/oniL6Q3+/XrtFdqMR8dob+SX48E=";

            await using var fluxzyInstance = await commandLineHost.Run();
            using var proxiedHttpClient = new ProxiedHttpClient(fluxzyInstance.ListenPort);

            var data = new {
                fileContent = Convert.ToBase64String(Enumerable.Repeat(55, 3300).Select(s => (byte) s).ToArray())
            };

            proxiedHttpClient.Client.DefaultRequestHeaders.Add("Cookie", "import-tool-session=" + cookieValue);
            proxiedHttpClient.Client.DefaultRequestHeaders.ExpectContinue = false;

            var response = await proxiedHttpClient.Client.PostAsJsonAsync(
                $"{TestConstants.GetHost(protocol)}/global-health-check", data);

            await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    public static class AggressiveCallProducer
    {
        public static async Task MakeAggressiveCall(string url, int listenPort, int bodyLength, bool abort)
        {
            using var handler = new HttpClientHandler {
                Proxy = new WebProxy($"http://127.0.0.1:{listenPort}")
            };

            var cancellationTokenSource = new CancellationTokenSource();

            using var client = new HttpClient(handler);

            var responseMessageTask = client.GetAsync(url, cancellationTokenSource.Token);
            var contentLength = -1;

            if (abort)

                // await Task.Delay(50);
                cancellationTokenSource.Cancel();

            try {
                var responseMessage = await responseMessageTask;

                contentLength = (await responseMessage.Content.ReadAsStreamAsync(cancellationTokenSource.Token))
                    .Drain();
            }
            catch (OperationCanceledException) {
                return;
            }

            if (!abort)
                Assert.Equal(bodyLength, contentLength);
        }
    }
}
