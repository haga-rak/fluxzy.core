// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using System.IO;
using Fluxzy.Misc.Streams;
using Fluxzy.Tests.Cli.Scaffolding;

namespace Fluxzy.Tests.Cli.Dissects
{
    public record RunResult(int ExitCode, Stream StandardOutput, Stream StandardError);

    public class DissectCommandTests : CommandBase
    {
        public DissectCommandTests()
            : base("dissect")
        {
        }

        [Theory]
        [InlineData(".artefacts/tests/pink-floyd")]
        [InlineData("_Files/Archives/pink-floyd.fxzy")]
        public async Task Read_Check_Count(string input)
        {
            var runResult = await InternalRun(input);

            var rawStdout = runResult.StandardOutput.ReadAsString();
            var stdOutLines = rawStdout.Split(new [] { Environment.NewLine }, 
                StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(0, runResult.ExitCode);
            Assert.Equal(16, stdOutLines.Length);
            Assert.Equal("99 - https://en.wikipedia.org/static/favicon/wikipedia.ico - 200", stdOutLines.Last());
        }

        [Fact]
        public async Task Read_Fxzy_Missing_Directory_Or_File()
        {
            var fileName = "_oHe/foo_bar";

            var runResult = await InternalRun(fileName);

            var rawStdout = runResult.StandardOutput.ReadAsString();
            var stdOutLines = rawStdout.Split(new [] { Environment.NewLine }, 
                StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(1, runResult.ExitCode);
        }

        [Theory]
        [MemberData(nameof(GetParam_Read_Fxzy_Check_Property))]
        public async Task Read_Fxzy_Check_Property(string input, int exchangeId, 
            string property, string value)
        {
            var runResult = await InternalRun(input, $"-f", $"{{{property}}}", "-i", exchangeId.ToString());

            var rawStdout = runResult.StandardOutput.ReadAsString().TrimEnd('\r', '\n');

            Assert.Equal(0, runResult.ExitCode);
            Assert.Equal(value, rawStdout);
        }

        [Theory]
        [InlineData("_Files/Archives/pink-floyd.fxzy", 92, "pcap-raw", 256012)]
        [InlineData("_Files/Archives/pink-floyd.fxzy", 92, "pcap", 257288)]
        [InlineData("_Files/Archives/pink-floyd.fxzy", 55, "request-body", 0)]
        [InlineData("_Files/Archives/pink-floyd.fxzy", 55, "response-body", 3264)]
        public async Task Read_Fxzy_Check_Property_Binary(
            string input, int exchangeId,
            string property, int expectedLength)
        {
            var runResult = await InternalRun(input, $"-f", $"{{{property}}}", "-i", exchangeId.ToString());
            runResult.StandardOutput.Seek(0, SeekOrigin.Begin);
            var actualLength = await  runResult.StandardOutput.DrainAsync(); 

            Assert.Equal(0, runResult.ExitCode);
            Assert.Equal(expectedLength, actualLength);
        }

        [Theory]
        [InlineData("_Files/Archives/pink-floyd.fxzy", 92, "pcap-raw", 256012)]
        [InlineData("_Files/Archives/pink-floyd.fxzy", 92, "pcap", 257288)]
        [InlineData("_Files/Archives/pink-floyd.fxzy", 55, "request-body", 0)]
        [InlineData("_Files/Archives/pink-floyd.fxzy", 55, "response-body", 3264)]
        public async Task Read_Fxzy_Check_Property_Binary_Output_File(
            string input, int exchangeId,
            string property, int expectedLength)
        {
            var tempFile = GetTempFile(); 

            var runResult = await InternalRun(input, $"-f", $"{{{property}}}", "-i", exchangeId.ToString(), 
                "-o", tempFile.FullName);
            
            Assert.Equal(0, runResult.ExitCode);
            Assert.Equal(expectedLength, tempFile.Length);
        }

        public static List<object[]> GetParam_Read_Fxzy_Check_Property()
        {
            var path = new[] { ".artefacts/tests/pink-floyd", "_Files/Archives/pink-floyd.fxzy" };

            var args = new object[][] {
                new object [3]{99, "status", "200"},
                new object [3]{99, "request-body", ""},
                new object [3]{99, "response-body-length", "1035"},
                new object [3]{99, "path", "/static/favicon/wikipedia.ico"},
                new object [3]{99, "invalid", "{invalid}"},
                new object [3]{99, "method", "GET"},
                new object [3]{99, "content-type", "img"},
                new object [3]{99, "authority", "en.wikipedia.org"},
                new object [3]{99, "host", "en.wikipedia.org"},
                new object [3]{99, "scheme", "https"},
                new object [3]{99, "http-version", "HTTP/2"},
                new object [3]{99, "id", "99"},
                new object [3]{99, "url", "https://en.wikipedia.org/static/favicon/wikipedia.ico"},
            };

            return args.SelectMany(x => path.Select(y => new object[] { y, x[0], x[1], x[2] })).ToList();
        }

    }
}
