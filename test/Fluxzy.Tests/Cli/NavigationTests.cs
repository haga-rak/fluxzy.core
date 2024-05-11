// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading.Tasks;
using Fluxzy.Cli;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class NavigationTests
    {
        [Theory]
        [InlineData("--version", true)]
        [InlineData("--help", true)]
        [InlineData("start --help", true)]
        [InlineData("dissect --help", true)]
        [InlineData("pack --help", true)]
        [InlineData("pack", false)]
        [InlineData("cert --help", true)]
        [InlineData("cert", false)]
        [InlineData("", false)]
        [InlineData("invalid command", false)]
        public async Task Version(string args, bool success)
        {
            var tabArgs = args.Split(' ');
            
            Environment.SetEnvironmentVariable("FLUXZY_NO_STDOUT", "1");
            var yes = await Program.Main(tabArgs);
            Environment.SetEnvironmentVariable("FLUXZY_NO_STDOUT", "");

            Assert.Equal(success, yes == 0);
        }
    }
}
