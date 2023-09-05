// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Xunit;

namespace Fluxzy.Tests.Misc
{
    public class AuthorityUtilityTests
    {
        [Theory]
        [InlineData("www.example.com:4456", true, "www.example.com", 4456)]
        [InlineData("www.example.com4456", false, null, 0)]
        [InlineData("::1:4456", true, "::1", 4456)]
        [InlineData("", false, null, 0)]
        [InlineData("www.example.com/4456", true, "www.example.com", 4456)]
        [InlineData("www.example.com4456", false, null, 0)]
        [InlineData("::1/4456", true, "::1", 4456)]
        public void Test_With_HostName(string input, bool expectedResult, string expectedHost, int expectedPort)
        {
            var result = Fluxzy.Utils.AuthorityUtility.TryParse(input, out var host, out var port);

            Assert.Equal(expectedResult, result);

            Assert.Equal(expectedHost, host);
            Assert.Equal(expectedPort, port);
        }
    }
}
