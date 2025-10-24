using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests.Cli;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Keylogs
{
    public class SslKeyLoggingTests : WithRuleOptionBase
    {
        [Fact]
        public async Task ValidateSslKeyLogExists()
        {
            var url = "https://www.fluxzy.io/favicon.ico";
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);

            var yamlContent = """
                               rules:
                               - filter:
                                   typeKind: anyFilter
                                 action :
                                   typeKind: noOpAction
                               """;

            var outputDirectory = "test_directory_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            
            var extraCommandLines = $"-b -c -d {outputDirectory}";

            var _ = await Exec(yamlContent, requestMessage,
                allowAutoRedirect: false, 
                extraCommandLineArgs: extraCommandLines);

            var allFiles = new DirectoryInfo(outputDirectory).EnumerateFiles("*.nsskeylog", SearchOption.AllDirectories)
                                                            .ToList();

            Assert.NotEmpty(allFiles);
            Assert.Single(allFiles);
        }
    }
}
