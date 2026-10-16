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
    public class DissectPlaceholderTests : CommandBase
    {
        private const string Archive = "_Files/Archives/pink-floyd.fxzy";

        public DissectPlaceholderTests()
            : base("dissect")
        {
        }

        [Theory]
        [InlineData("response-header:content-type", "image/vnd.microsoft.icon")]
        [InlineData("response-header:Content-Length", "1035")]
        [InlineData("response-header:missing", "")]
        [InlineData("request-header::method", "GET")]
        [InlineData("connection-id", "14")]
        [InlineData("remote-ip", "185.15.58.224")]
        [InlineData("tls-version", "Tls13")]
        [InlineData("alpn", "h2")]
        [InlineData("ttfb", "13.84")]
        [InlineData("duration", "13.95")]
        [InlineData("start-time", "2023-10-07T00:00:26.3346049Z")]
        [InlineData("tags", "")]
        [InlineData("comment", "")]
        public async Task Placeholder_Value(string placeholder, string expected)
        {
            var runResult = await InternalRun(Archive, "-i", "99", "-f", $"{{{placeholder}}}");

            Assert.Equal(0, runResult.ExitCode);
            Assert.Equal(expected, runResult.StandardOutput.ReadAsString().TrimEnd('\r', '\n'));
        }

        [Fact]
        public async Task Response_Headers_One_Per_Line()
        {
            var runResult = await InternalRun(Archive, "-i", "99", "-f", "{response-headers}");

            Assert.Equal(0, runResult.ExitCode);

            var lines = runResult.StandardOutput.ReadAsString()
                                 .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(l => l.TrimEnd('\r'))
                                 .ToList();

            Assert.Equal(":status: 200", lines[0]);
            Assert.Contains("content-encoding: gzip", lines);
            Assert.Contains("content-length: 1035", lines);
        }

        [Fact]
        public async Task Request_Headers_Present()
        {
            var runResult = await InternalRun(Archive, "-i", "99", "-f", "{request-headers}");

            Assert.Equal(0, runResult.ExitCode);
            Assert.Contains(":authority: en.wikipedia.org", runResult.StandardOutput.ReadAsString());
        }

        [Fact]
        public async Task Decoded_Body_Gzip()
        {
            var runResult = await InternalRun(Archive, "-i", "99", "-f", "{response-body-decoded}");
            runResult.StandardOutput.Seek(0, System.IO.SeekOrigin.Begin);
            var length = await runResult.StandardOutput.DrainAsync();

            Assert.Equal(0, runResult.ExitCode);
            Assert.Equal(2734, length);
        }

        [Fact]
        public async Task Max_Body_Bytes_Truncates_With_Marker()
        {
            var runResult = await InternalRun(Archive, "-i", "47", "-f", "{response-body-decoded}",
                "--max-body-bytes", "60");

            Assert.Equal(0, runResult.ExitCode);

            var text = runResult.StandardOutput.ReadAsString();

            Assert.EndsWith("[truncated: 60 of 7899 bytes]", text.TrimEnd('\r', '\n'));
            Assert.Equal(60, text.IndexOf("\n[truncated", StringComparison.Ordinal));
        }

        [Fact]
        public async Task Max_Body_Bytes_No_Marker_When_Small()
        {
            var runResult = await InternalRun(Archive, "-i", "99", "-f", "{response-body}",
                "--max-body-bytes", "100000");
            runResult.StandardOutput.Seek(0, System.IO.SeekOrigin.Begin);
            var length = await runResult.StandardOutput.DrainAsync();

            Assert.Equal(1035, length);
        }

        [Fact]
        public async Task Json_With_Headers()
        {
            var runResult = await InternalRun(Archive, "-i", "99", "--ndjson", "--with-headers");

            Assert.Equal(0, runResult.ExitCode);

            var root = JsonDocument.Parse(runResult.StandardOutput.ReadAsString().Trim()).RootElement;
            var response = root.GetProperty("responseHeaders").EnumerateArray().ToList();

            Assert.Contains(response, h => h.GetProperty("name").GetString() == "content-length"
                                           && h.GetProperty("value").GetString() == "1035");
            Assert.True(root.GetProperty("requestHeaders").GetArrayLength() > 0);
        }

        [Fact]
        public async Task Json_Without_Headers_By_Default()
        {
            var runResult = await InternalRun(Archive, "-i", "99", "--ndjson");

            var root = JsonDocument.Parse(runResult.StandardOutput.ReadAsString().Trim()).RootElement;

            Assert.False(root.TryGetProperty("responseHeaders", out _));
        }
    }
}
