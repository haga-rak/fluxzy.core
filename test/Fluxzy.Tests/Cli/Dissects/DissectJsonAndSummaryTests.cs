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
    public class DissectJsonAndSummaryTests : CommandBase
    {
        private const string Archive = "_Files/Archives/pink-floyd.fxzy";

        public DissectJsonAndSummaryTests()
            : base("dissect")
        {
        }

        [Fact]
        public async Task Json_Single_Exchange()
        {
            var runResult = await InternalRun(Archive, "--json", "-i", "99");

            Assert.Equal(0, runResult.ExitCode);

            using var doc = JsonDocument.Parse(runResult.StandardOutput.ReadAsString());
            var items = doc.RootElement.EnumerateArray().ToList();

            Assert.Single(items);

            var e = items[0];

            Assert.Equal(99, e.GetProperty("id").GetInt32());
            Assert.Equal(200, e.GetProperty("status").GetInt32());
            Assert.Equal("GET", e.GetProperty("method").GetString());
            Assert.Equal("en.wikipedia.org", e.GetProperty("authority").GetString());
            Assert.Equal("/static/favicon/wikipedia.ico", e.GetProperty("path").GetString());
            Assert.Equal("HTTP/2", e.GetProperty("httpVersion").GetString());
            Assert.Equal(1035, e.GetProperty("responseBodyLength").GetInt64());
            Assert.Equal(0, e.GetProperty("requestBodyLength").GetInt64());
            Assert.True(e.GetProperty("hasPcap").GetBoolean());
            Assert.Equal("h2", e.GetProperty("alpn").GetString());
            Assert.True(e.GetProperty("ttfbMs").GetDouble() > 0);
        }

        [Fact]
        public async Task Json_All_Exchanges()
        {
            var runResult = await InternalRun(Archive, "--json");

            Assert.Equal(0, runResult.ExitCode);

            using var doc = JsonDocument.Parse(runResult.StandardOutput.ReadAsString());

            Assert.Equal(18, doc.RootElement.GetArrayLength());
        }

        [Fact]
        public async Task NdJson_Two_Exchanges()
        {
            var runResult = await InternalRun(Archive, "--ndjson", "-i", "99,186");

            Assert.Equal(0, runResult.ExitCode);

            var lines = runResult.StandardOutput.ReadAsString()
                                 .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(l => l.TrimEnd('\r'))
                                 .ToList();

            Assert.Equal(2, lines.Count);

            var ids = lines.Select(l => JsonDocument.Parse(l).RootElement.GetProperty("id").GetInt32()).ToList();

            Assert.Equal(new[] { 99, 186 }, ids);
        }

        [Fact]
        public async Task Summary_Text()
        {
            var runResult = await InternalRun("summary", Archive);

            Assert.Equal(0, runResult.ExitCode);

            var text = runResult.StandardOutput.ReadAsString();

            Assert.Contains("Exchanges       : 18", text);
            Assert.Contains("Connections     : 4", text);
            Assert.Contains("en.wikipedia.org", text);
            Assert.Contains("200=17", text);
        }

        [Fact]
        public async Task Summary_Json()
        {
            var runResult = await InternalRun("summary", Archive, "--json", "--max-hosts", "2");

            Assert.Equal(0, runResult.ExitCode);

            using var doc = JsonDocument.Parse(runResult.StandardOutput.ReadAsString());
            var root = doc.RootElement;

            Assert.Equal("file", root.GetProperty("sourceType").GetString());
            Assert.Equal(18, root.GetProperty("exchanges").GetProperty("total").GetInt32());
            Assert.Equal(4, root.GetProperty("connections").GetProperty("total").GetInt32());
            Assert.Equal(17, root.GetProperty("statusCodes").GetProperty("200").GetInt32());
            Assert.Equal(18, root.GetProperty("methods").GetProperty("GET").GetInt32());
            Assert.Equal(2, root.GetProperty("hosts").GetArrayLength());
            Assert.Equal("en.wikipedia.org", root.GetProperty("hosts")[0].GetProperty("host").GetString());
        }

        [Fact]
        public async Task Summary_Missing_Archive()
        {
            var runResult = await InternalRun("summary", "_oHe/foo_bar");

            Assert.Equal(1, runResult.ExitCode);
        }
    }
}
