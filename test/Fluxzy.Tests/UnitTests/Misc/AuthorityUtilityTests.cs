// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    public class AuthorityUtilityTests
    {
        [Theory]
        [InlineData("www.example.com:4456", true, "www.example.com", 4456)]
        [InlineData("www.example.com4456", false, null, 0)]
        [InlineData("::1:4456", true, "::1", 4456)]
        [InlineData("", false, null, 0)]
        [InlineData("www.example.com/4456", true, "www.example.com", 4456)]
        [InlineData("::1/4456", true, "::1", 4456)]
        [InlineData("www.example.com:445325", false, null, 0)]
        [InlineData(":8080", false, null, 0)]
        [InlineData("/8080", false, null, 0)]
        [InlineData("8080:", false, null, 0)]
        [InlineData("8080/", false, null, 0)]
        public void Test_With_HostName(string input, bool expectedResult, string? expectedHost, int expectedPort)
        {
            var result = Utils.AuthorityUtility.TryParse(input, out var host, out var port);

            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedHost, host);
            Assert.Equal(expectedPort, port);
        }

        [Theory]
        [InlineData("www.example.com:4456", false, null, 0)]
        [InlineData("127.0.0.1:4456", true, "127.0.0.1", 4456)]
        [InlineData("0.0.0.0:4456", true, "0.0.0.0", 4456)]
        [InlineData(":::4456", true, "::", 4456)]
        [InlineData("2001:db8:::4456", true, "2001:db8::", 4456)]
        [InlineData("::1234:5678:4456", true, "::1234:5678", 4456)]
        [InlineData("2001:db8:3333:4444:5555:6666:7777:8888:4456", true, "2001:db8:3333:4444:5555:6666:7777:8888", 4456)]
        public void Test_With_Ip(string input, bool expectedResult, string? rawIp, int expectedPort)
        {
            var expectedIp = rawIp == null ? null : IPAddress.Parse(rawIp);

            var result = Utils.AuthorityUtility.TryParseIp(input, out var ipAddress, out var port);

            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedIp, ipAddress);
            Assert.Equal(expectedPort, port);
        }
    }
}
