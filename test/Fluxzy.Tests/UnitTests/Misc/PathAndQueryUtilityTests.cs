// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Misc;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    public class PathAndQueryUtilityTests
    {
        [Theory]
        [InlineData("http://eu.httpbin.org", "/")]
        [InlineData("http://eu.httpbin.org/", "/")]
        [InlineData(TestConstants.TestDomain, "/ip")]
        [InlineData("https://www.fluxzy.io/download", "/download")]
        [InlineData("http://www.fluxzy.io/download", "/download")]
        [InlineData("http://www.fluxzy.io/download://a", "/download://a")]
        [InlineData("/immediate-path", "/immediate-path")]
        [InlineData("/", "/")]
        [InlineData("httpmypath", "httpmypath")]
        public void Parse(string input, string expected)
        {
            // Arrange
            var inputSpan = input.AsSpan();
            var expectedSpan = expected.AsSpan();

            // Act
            var result = PathAndQueryUtility.Parse(inputSpan);

            // Assert
            Assert.Equal(expectedSpan, result, false);
        }
    }
}
