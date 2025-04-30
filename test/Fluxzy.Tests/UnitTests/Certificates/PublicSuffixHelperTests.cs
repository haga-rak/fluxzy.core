// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Certificates;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Certificates
{
    public class PublicSuffixHelperTests
    {
        [Theory]
        [InlineData("tpl.co.uk", "tpl.co.uk")]
        [InlineData("www.tpl.co.uk", "tpl.co.uk")]
        [InlineData("gogol.uk", "gogol.uk")]
        [InlineData("www.gogol.uk", "gogol.uk")]
        [InlineData("fluxzy.io", "fluxzy.io")]
        [InlineData("a.b.c.d.e.fluxzy.io", "b.c.d.e.fluxzy.io")]
        [InlineData("anotherlevel.www.fluxzy.io", "www.fluxzy.io")]
        [InlineData("dododo.lololo", "dododo.lololo")]
        [InlineData("www.dododo.lololo", "dododo.lololo")]
        [InlineData("dododo", "dododo")]
        [InlineData("io", "io")]
        [InlineData("172.16.12.123", "172.16.12.123")]
        public void TestRootDomain(string input, string outputFlat)
        {
            var result = PublicSuffixHelper.GetRootDomain(input);
            Assert.Equal(outputFlat, result);
        }
    }
}
