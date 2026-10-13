// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Fluxzy.Tests.Cli.Scaffolding;
using Xunit;

namespace Fluxzy.Tests.Cli.Dissects
{
    public class DissectFilterTests : CommandBase
    {
        private const string Archive = "_Files/Archives/pink-floyd.fxzy";

        public DissectFilterTests()
            : base("dissect")
        {
        }

        [Theory]
        [InlineData("--host", "*.wikimedia.org", 8)]
        [InlineData("--host", "EN.WIKIPEDIA.ORG", 8)]
        [InlineData("--host", "nomatch.example", 0)]
        [InlineData("--path", "/static/*", 3)]
        [InlineData("--path", "/\\.ico$/", 1)]
        [InlineData("--method", "get", 18)]
        [InlineData("--method", "POST,PUT", 0)]
        [InlineData("--status", "200", 17)]
        [InlineData("--status", "1xx", 1)]
        [InlineData("--status", "100-199,101", 1)]
        [InlineData("--status", "4xx,5xx", 0)]
        [InlineData("--content-type", "CSS", 2)]
        [InlineData("--limit", "3", 3)]
        [InlineData("--offset", "16", 2)]
        public async Task Filter_Count(string option, string value, int expected)
        {
            var runResult = await InternalRun(Archive, "--ndjson", option, value);

            Assert.Equal(0, runResult.ExitCode);

            var lines = runResult.StandardOutput.ReadAsString()
                                 .Split('\n', StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(expected, lines.Length);
        }

        [Fact]
        public async Task Filters_Compose_With_Id_And_Limit()
        {
            var runResult = await InternalRun(Archive, "--ndjson",
                "--host", "upload.wikimedia.org", "--status", "2xx", "--offset", "1", "--limit", "2");

            Assert.Equal(0, runResult.ExitCode);

            var ids = runResult.StandardOutput.ReadAsString()
                               .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                               .Select(l => JsonDocument.Parse(l).RootElement)
                               .ToList();

            Assert.Equal(2, ids.Count);
            Assert.All(ids, e => Assert.Equal("upload.wikimedia.org", e.GetProperty("authority").GetString()));
        }

        [Fact]
        public async Task Filter_Works_With_Format_Output()
        {
            var runResult = await InternalRun(Archive, "-f", "{id}", "--path", "/\\.ico$/", "-u");

            Assert.Equal(0, runResult.ExitCode);
            Assert.Equal("99", runResult.StandardOutput.ReadAsString().Trim());
        }

        [Fact]
        public async Task Invalid_Status_Filter_Fails()
        {
            var runResult = await InternalRun(Archive, "--status", "bogus");

            Assert.Equal(1, runResult.ExitCode);
        }
    }
}
