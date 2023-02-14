// Copyright © 2023 Haga RAKOTOHARIVELO

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Misc.Streams;
using Fluxzy.Readers;
using Fluxzy.Tests.Cli.Scaffolding;
using Fluxzy.Tests.Common;
using Fluxzy.Utils.Curl;
using Xunit;

namespace Fluxzy.Tests
{
    public class CurlExportTests
    {
        [Theory]
        [InlineData("GET", TestPayloadType.FlatText)]
        [InlineData("POST", TestPayloadType.FlatText)]
        [InlineData("POST", TestPayloadType.FormContentEncoded)]
        public async Task Run_And_Check_Curl_Response_Against_HttpClient(string methodString, 
            TestPayloadType payloadType)
        {
            var converter = new CurlRequestConverter();
            
            var rootDir = Guid.NewGuid().ToString();
            var directoryName = $"{rootDir}/httpclient-test";

            try {

                var quickTestResult = await DoRequestThroughHttpClient(directoryName, 
                    new HttpMethod(methodString), payloadType);
                
                using var archiveReader = quickTestResult.ArchiveReader;

                var curlDirectoryOutput = $"{rootDir}/curl-test"; 

                var commandLine = "start -l 127.0.0.1/0";
                commandLine += $" -d {curlDirectoryOutput}";

                var commandLineHost = new FluxzyCommandLineHost(commandLine);

                await using (var fluxzyInstance = await commandLineHost.Run()) {
                    var commandResult = converter.BuildCurlRequest(archiveReader, quickTestResult.ExchangeInfo, new CurlProxyConfiguration(
                        "127.0.0.1", fluxzyInstance.ListenPort));

                    var res = CurlUtility.RunCurl(commandResult.FlatCommandLineWithProxyArgs,
                        out var stdout, out var stderr);
                }

                using var curlArchiveReader = new DirectoryArchiveReader(curlDirectoryOutput);

                var exchanges = curlArchiveReader.ReadAllExchanges().ToList();
                
                var httpClientExchange = quickTestResult.ExchangeInfo;
                var curlExchange = exchanges.FirstOrDefault()!;

                Assert.NotNull(curlExchange);

                var curlFlatHeader = string.Join("\r\n",
                    curlExchange
                        .GetRequestHeaders()
                        .OrderBy(r => r.Name.ToString()).Select(s => $"{s.Name} : {s.Value}"));
                
                var httpClientFlatHeader = string.Join("\r\n",
                    httpClientExchange
                        .GetRequestHeaders()
                        .OrderBy(r => r.Name.ToString()).Select(s => $"{s.Name} : {s.Value}"));

                Assert.Equal(httpClientFlatHeader, curlFlatHeader);
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

                if (method == HttpMethod.Post) {
                    
                    if (payloadType == TestPayloadType.FlatText) {
                        requestMessage.Content = new StringContent("Some flatString", Encoding.UTF8); 
                    }

                    if (payloadType == TestPayloadType.FormContentEncoded) {
                        requestMessage.Content = new FormUrlEncodedContent(new Dictionary<string, string>() {
                            {"A", "B"},
                            {"C", "p"},
                            {"d", "g"},
                            {"f", "r"},
                        }); 
                    }
                }

                requestMessage.Headers.Add("x-header-test", "123456");

                // Act 
                using var response = await proxiedHttpClient.Client.SendAsync(requestMessage);

                await response.Content.ReadAsStringAsync();
                
            }

            // Assert outputDirectory content

            IArchiveReader archiveReader = new DirectoryArchiveReader(directoryName);

            var exchanges = archiveReader.ReadAllExchanges().ToList();
            var connections = archiveReader.ReadAllConnections().ToList();

            var exchange = exchanges.FirstOrDefault()!;

            var connection = connections.First();

            var quickTestResult = new QuickTestResult(archiveReader, exchange);

            Assert.NotNull(exchange);
            Assert.Equal(0, await commandLineHost.ExitCode);
            Assert.Single(exchanges);
            Assert.Single(connections);

            Assert.Equal(200, exchange.StatusCode);
            Assert.Equal(connection.Id, exchange.ConnectionId);
            return quickTestResult;
        }
    }

    public enum TestPayloadType
    {
        FlatText = 1 ,
        FormContentEncoded = 2, 
    }
    
    internal class QuickTestResult
    {
        public QuickTestResult(IArchiveReader archiveReader, ExchangeInfo exchangeInfo)
        {
            ArchiveReader = archiveReader;
            ExchangeInfo = exchangeInfo;
        }

        public IArchiveReader ArchiveReader { get; }

        public ExchangeInfo ExchangeInfo { get;  }

        
    }
}
