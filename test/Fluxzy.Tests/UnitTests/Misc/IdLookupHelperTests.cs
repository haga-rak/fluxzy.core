// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using Fluxzy.Misc;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    public class IdLookupHelperTests
    {
        [Theory]
        [InlineData("1", "1")]
        [InlineData("1-5", "1,2,3,4,5")]
        [InlineData("", "")]
        [InlineData("invalid entry9", "")]
        [InlineData("1,2,3,4,5", "1,2,3,4,5")]
        [InlineData("1,2,3,a,4,5", "1,2,3,4,5")]
        [InlineData("1,2,10-12,19", "1,2,10,11,12,19")]
        public void ParsePattern(string pattern, string expectedValues)
        {
            var expectedValueLists = 
                expectedValues.Split(new [] {","}, StringSplitOptions.RemoveEmptyEntries)
                              .Select(int.Parse)
                              .ToHashSet();

            var actualResult = IdLookupHelper.ParsePattern(pattern);

            Assert.Equal(expectedValueLists, actualResult);
        }
    }
}
