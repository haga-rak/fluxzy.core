// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Tests.Cli;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Pcap
{
    public class RecoverPcapAfterNxFailure : WithRuleOptionBase
    {
        [Fact]
        public async Task SslNegotiationFail()
        {
            var url = "https://www.fluxzy.io/favicon.ico";
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);

            var yamlContent = """
                              rules:
                              - filter:
                                  typeKind: anyFilter
                                action :
                                  typeKind: ForceTlsVersionAction
                                  sslProtocols: tls
                              """;

            var outputDirectory = "ValidateSslKeyLogExists_" + DateTime.Now.ToString("yyyyMMddHHmmss");

            var extraCommandLines = $"-b -c -d {outputDirectory}";

            var res = await Exec(yamlContent, requestMessage,
                allowAutoRedirect: false,
                extraCommandLineArgs: extraCommandLines);

            await (await res.Content.ReadAsStreamAsync()).CopyToAsync(Stream.Null);
            await ReleaseAll();

            var allFiles = new DirectoryInfo(outputDirectory).EnumerateFiles("*.pcapng", SearchOption.AllDirectories)
                                                             .ToList();
            
            Assert.NotEmpty(allFiles);
            Assert.Single(allFiles);
            Assert.True(allFiles.First().Length > 0);
        }

        [Fact]
        public async Task RefusedConnection()
        {
            var url = "https://www.fluxzy.io:100/favicon.ico";
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);

            var yamlContent = """
                              rules:
                              - filter:
                                  typeKind: anyFilter
                                action :
                                  typeKind: noOpAction
                              """;

            var outputDirectory = "RefusedConnection_" + DateTime.Now.ToString("yyyyMMddHHmmss");

            var extraCommandLines = $"-b -c -d {outputDirectory}";

            var res = await Exec(yamlContent, requestMessage,
                allowAutoRedirect: false,
                extraCommandLineArgs: extraCommandLines);

            await (await res.Content.ReadAsStreamAsync()).CopyToAsync(Stream.Null);
            await ReleaseAll();

            var allFiles = new DirectoryInfo(outputDirectory).EnumerateFiles("*.pcapng", SearchOption.AllDirectories)
                                                             .ToList();
            
            Assert.NotEmpty(allFiles);
            Assert.Single(allFiles);
            Assert.True(allFiles.First().Length > 0);
        }

        [Fact]
        public async Task InvalidProtocol()
        {
            var url = "http://www.fluxzy.io:443/favicon.ico";
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);

            var yamlContent = """
                              rules:
                              - filter:
                                  typeKind: anyFilter
                                action :
                                  typeKind: noOpAction
                              """;

            var outputDirectory = "InvalidProtocol_" + DateTime.Now.ToString("yyyyMMddHHmmss");

            var extraCommandLines = $"-b -c -d {outputDirectory}";

            var res = await Exec(yamlContent, requestMessage,
                allowAutoRedirect: false,
                extraCommandLineArgs: extraCommandLines);

            await (await res.Content.ReadAsStreamAsync()).CopyToAsync(Stream.Null);
            await ReleaseAll();

            var allFiles = new DirectoryInfo(outputDirectory).EnumerateFiles("*.pcapng", SearchOption.AllDirectories)
                                                             .ToList();
            
            Assert.NotEmpty(allFiles);
            Assert.Single(allFiles);
            Assert.True(allFiles.First().Length > 0);
        }
    }
}
