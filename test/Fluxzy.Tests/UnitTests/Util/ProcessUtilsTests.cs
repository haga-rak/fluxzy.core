// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Misc;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Util
{
    public class ProcessUtilsTests
    {
        [Fact]
        public void RunAndExpectZero()
        {
            var result = ProcessUtils.RunAndExpectZero("true", "");
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void RunAndExpectZero_Fail()
        {
            var result = ProcessUtils.RunAndExpectZero("false", "");
            Assert.Null(result);
        }

        [Theory]
        [InlineData("grep hello", "hello world", "hello world", true, false)]
        [InlineData("grep hello", "hello world\nanother line\nend", "hello world", true, false)]
        [InlineData("grep goodbye", "hello world", "", false, false)]
        [InlineData("grep goodbye", "hello world", "", false, true)]
        public async Task QuickRunAsync_Grep(string fullCommand, string stdInContent, 
            string stdout, bool success, bool throwOnFail)
        {
            var stdInStream = new MemoryStream(Encoding.UTF8.GetBytes(stdInContent));

            async Task DoTest()
            {
                var result = await ProcessUtils.QuickRunAsync(fullCommand, stdInStream, throwOnFail);

                Assert.NotNull(result);
                Assert.Equal(success, result.ExitCode == 0);
                Assert.Equal(stdout, result.StandardOutputMessage!.Trim('\r', '\n'));
            }

            if (throwOnFail && !success) {
                await Assert.ThrowsAsync<InvalidOperationException>(DoTest);
            }
            else {
                await DoTest();
            }
        }

        [Theory]
        [InlineData("grep hello", "hello world", "hello world", true, false)]
        [InlineData("grep hello", "hello world\nanother line\nend", "hello world", true, false)]
        [InlineData("grep goodbye", "hello world", "", false, false)]
        [InlineData("grep goodbye", "hello world", "", false, true)]
        public void QuickRun_Grep(string fullCommand, string stdInContent, 
            string stdout, bool success, bool throwOnFail)
        {
            var stdInStream = new MemoryStream(Encoding.UTF8.GetBytes(stdInContent));

            void DoTest()
            {
                var result = ProcessUtils.QuickRun(fullCommand, stdInStream, throwOnFail);

                Assert.NotNull(result);
                Assert.Equal(success, result.ExitCode == 0);
                Assert.Equal(stdout, result.StandardOutputMessage!.Trim('\r', '\n'));
            }

            if (throwOnFail && !success) {
                Assert.Throws<InvalidOperationException>(DoTest);
            }
            else {
                DoTest();
            }
        }
    }
}
