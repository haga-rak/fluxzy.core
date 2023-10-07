// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using System.IO;

namespace Fluxzy.Tests.Cli.Dissects
{
    public record RunResult(int ExitCode, Stream StandardOutput, Stream StandardError);

    public class DissectCommandTests : DissectCommandBase
    {
        [Theory]
        [InlineData(".artefacts/tests/pink-floyd")]
        [InlineData("_Files/Archives/pink-floyd.fxzy")]
        public async Task Read_Fxzy_Check_Count(string input)
        {
            var runResult = await InternalRun(input);

            var rawStdout = runResult.StandardOutput.ReadAsString();
            var stdOutLines = rawStdout.Split(new [] { Environment.NewLine }, 
                StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(0, runResult.ExitCode);
            Assert.Equal(45, stdOutLines.Length);
            Assert.Equal("99 - https://en.wikipedia.org/static/favicon/wikipedia.ico - 200", stdOutLines.Last());
        }

        [Fact]
        public async Task Read_Fxzy_Absent_Directory()
        {
            var fileName = "_oHe/foo_bar";

            var runResult = await InternalRun(fileName);

            var rawStdout = runResult.StandardOutput.ReadAsString();
            var stdOutLines = rawStdout.Split(new [] { Environment.NewLine }, 
                StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(1, runResult.ExitCode);
        }

        [Theory]
        [InlineData("_Files/Archives/pink-floyd.fxzy", 99, "status", "200")]
        [InlineData("_Files/Archives/pink-floyd.fxzy", 99, "request-body", "")]
        [InlineData("_Files/Archives/pink-floyd.fxzy", 99, "response-body-length", "1035")]
        [InlineData("_Files/Archives/pink-floyd.fxzy", 99, "path", "/static/favicon/wikipedia.ico")]
        [InlineData("_Files/Archives/pink-floyd.fxzy", 99, "invalid", "{invalid}")]
        [InlineData("_Files/Archives/pink-floyd.fxzy", 99, "method", "GET")]
        [InlineData("_Files/Archives/pink-floyd.fxzy", 99, "content-type", "img")]
        [InlineData("_Files/Archives/pink-floyd.fxzy", 99, "authority", "en.wikipedia.org")]
        [InlineData("_Files/Archives/pink-floyd.fxzy", 99, "host", "en.wikipedia.org")]
        [InlineData("_Files/Archives/pink-floyd.fxzy", 99, "scheme", "https")]
        [InlineData("_Files/Archives/pink-floyd.fxzy", 99, "http-version", "HTTP/2")]
        public async Task Read_Fxzy_Check_Property(string input, string exchangeId, 
            string property, string value)
        {
            var runResult = await InternalRun(input, $"-f", $"{{{property}}}", "-i", exchangeId);

            var rawStdout = runResult.StandardOutput.ReadAsString().TrimEnd('\r', '\n');

            Assert.Equal(0, runResult.ExitCode);
            Assert.Equal(value, rawStdout);
        }
        
    }
}
