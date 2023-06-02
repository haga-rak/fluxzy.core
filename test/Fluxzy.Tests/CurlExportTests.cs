// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Readers;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Tests.Cli.Scaffolding;
using Fluxzy.Utils.Curl;
using Xunit;

namespace Fluxzy.Tests
{
    public class CurlExportTests
    {
        [Theory]
        [InlineData("GET", TestPayloadType.FlatText)]
        [InlineData("POST", TestPayloadType.FlatText)]
        [InlineData("PUT", TestPayloadType.FormContentEncoded)]
        [InlineData("POST", TestPayloadType.Binary)]
        [InlineData("POST", TestPayloadType.BinarySmall)]
        public async Task Compare_Curl_W_HttpClient(
            string methodString,
            TestPayloadType payloadType)
        {
            var rootDir = $"{nameof(Compare_Curl_W_HttpClient)}{Guid.NewGuid()}";
            var directoryName = $"{rootDir}/http-client-test";
            var tempPath = $"{rootDir}/curl-temp";
            var folderManagement = new CurlExportFolderManagement(tempPath);
            var converter = new CurlRequestConverter(folderManagement);

            try {
                var quickTestResult = await DoRequestThroughHttpClient(directoryName,
                    new HttpMethod(methodString), payloadType);

                using var archiveReader = quickTestResult.ArchiveReader;

                var curlDirectoryOutput = $"{rootDir}/curl-test";

                var commandLine = "start -l 127.0.0.1/0";
                commandLine += $" -d {curlDirectoryOutput}";

                var commandLineHost = new FluxzyCommandLineHost(commandLine);

                await using (var fluxzyInstance = await commandLineHost.Run()) {
                    var commandResult = converter.BuildCurlRequest(archiveReader, quickTestResult.ExchangeInfo,
                        new CurlProxyConfiguration(
                            "127.0.0.1", fluxzyInstance.ListenPort));

                    var curlExecutionSuccess = await CurlUtility.RunCurl(
                        commandResult.GetProcessCompatibleArgs(),
                        folderManagement.TemporaryPath);

                    Assert.True(curlExecutionSuccess);
                }

                using var curlArchiveReader = new DirectoryArchiveReader(curlDirectoryOutput);

                var exchanges = curlArchiveReader.ReadAllExchanges().ToList();

                var httpClientExchange = quickTestResult.ExchangeInfo;
                var curlExchange = exchanges.FirstOrDefault()!;

                Assert.NotNull(curlExchange);

                await using var curlRequestBodyStream = curlArchiveReader.GetRequestBody(curlExchange.Id);
                await using var httpClientRequestBodyStream = archiveReader.GetRequestBody(curlExchange.Id);

                var curlFlatHeader = string.Join("\r\n",
                    curlExchange
                        .GetRequestHeaders()
                        .Where(h => !h.Name.Span.Equals("Expect", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(r => r.Name.ToString()).Select(s => $"{s.Name} : {s.Value}"));

                var httpClientFlatHeader = string.Join("\r\n",
                    httpClientExchange
                        .GetRequestHeaders()
                        .Where(h => !h.Name.Span.Equals("Expect", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(r => r.Name.ToString()).Select(s => $"{s.Name} : {s.Value}"));

                Assert.Equal(httpClientFlatHeader, curlFlatHeader);
                Assert.Equal(httpClientRequestBodyStream?.DrainAndSha1(), curlRequestBodyStream?.DrainAndSha1());
            }
            finally {
                if (Directory.Exists(rootDir))
                    Directory.Delete(rootDir, true);
            }
        }

        private static async Task<QuickTestResult> DoRequestThroughHttpClient(
            string directoryName, HttpMethod method, TestPayloadType payloadType)
        {
            // Arrange 
            var protocol = "http2";

            var commandLine = "start -l 127.0.0.1/0";
            commandLine += $" -d {directoryName}";

            var commandLineHost = new FluxzyCommandLineHost(commandLine);

            await using (var fluxzyInstance = await commandLineHost.Run()) {
                using var proxiedHttpClient = new ProxiedHttpClient(fluxzyInstance.ListenPort);

                var requestMessage = new HttpRequestMessage(method,
                    $"{TestConstants.GetHost(protocol)}/global-health-check");

                if (method != HttpMethod.Get) {
                    if (payloadType == TestPayloadType.FlatText)
                        requestMessage.Content = new StringContent("Some flatString", Encoding.UTF8);

                    if (payloadType == TestPayloadType.FormContentEncoded) {
                        requestMessage.Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                            { "A", "B" },
                            { "C", "p" },
                            { "d", "g" },
                            { "f", "r" }
                        });
                    }

                    if (payloadType == TestPayloadType.Binary) {
                        requestMessage.Content =
                            new StreamContent(new RandomDataStream(9, 1024 * 9 + 5, true));
                    }

                    if (payloadType == TestPayloadType.BinarySmall) {
                        requestMessage.Content =
                            new StreamContent(new RandomDataStream(9, 1024 + 5, true));
                    }
                }

                requestMessage.Headers.Add("x-header-test", "123456");
                requestMessage.Headers.Add("x-header-test-b", "12\"3456");
                requestMessage.Headers.Add("x-header-test-c", "12'3456");

                // Act 
                using var response = await proxiedHttpClient.Client.SendAsync(requestMessage);

                await response.Content.ReadAsStringAsync();
            }

            // Assert outputDirectory content

            IArchiveReader archiveReader = new DirectoryArchiveReader(directoryName);

            var exchanges = archiveReader.ReadAllExchanges().ToList();

            var exchange = exchanges.FirstOrDefault()!;

            var quickTestResult = new QuickTestResult(archiveReader, exchange);

            await commandLineHost.ExitCode;

            return quickTestResult;
        }
    }

    public enum TestPayloadType
    {
        FlatText = 1,
        FormContentEncoded = 2,
        Binary = 3,
        BinarySmall = 4
    }

    internal class QuickTestResult
    {
        public QuickTestResult(IArchiveReader archiveReader, ExchangeInfo exchangeInfo)
        {
            ArchiveReader = archiveReader;
            ExchangeInfo = exchangeInfo;
        }

        public IArchiveReader ArchiveReader { get; }

        public ExchangeInfo ExchangeInfo { get; }
    }
}
