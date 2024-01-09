// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Utils;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Util
{
    public class SubdomainUtilityTests
    {
        [Theory]
        [InlineData("www.google.com", "google.com")]
        [InlineData("google.com", "google.com")]
        [InlineData("google", null)]
        [InlineData("", null)]
        public void TryGetSubDomain(string input, string? expected)
        {
            var result = SubdomainUtility.TryGetSubDomain(input, out var subDomain);

            if (expected != null) {
                Assert.True(result);
                Assert.Equal(expected, subDomain);
            }
            else {
                Assert.False(result);
                Assert.Null(subDomain);
            }
        }
    }
}
