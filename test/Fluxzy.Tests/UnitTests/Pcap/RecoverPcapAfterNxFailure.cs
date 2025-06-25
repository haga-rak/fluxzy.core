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
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SslNegotiationFail(bool useBouncyCastle)
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

            var outputDirectory = "ValidateSslKeyLogExists_" + DateTime.Now.ToString("yyyyMMddHHmmss") + Guid.NewGuid();

            var extraCommandLines = $"{(useBouncyCastle ? "-b" : "")} -c -d {outputDirectory}";

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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task RefusedConnection(bool useBouncyCastle)
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

            var outputDirectory = "RefusedConnection_" + DateTime.Now.ToString("yyyyMMddHHmmss") + Guid.NewGuid(); 

            var extraCommandLines = $"{(useBouncyCastle ? "-b" : "")} -c -d {outputDirectory}";

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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task InvalidProtocol(bool useBouncyCastle)
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

            var outputDirectory = "InvalidProtocol_" + DateTime.Now.ToString("yyyyMMddHHmmss") + Guid.NewGuid();

            var extraCommandLines = $"{(useBouncyCastle ? "-b" : "")} -c -d {outputDirectory}";

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
