// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Text;
using Fluxzy.Misc.Streams;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    public class DetectHtmlTagOpeningMatcherTests
    {
        [Theory]
        [InlineData("<html><head><title>", "head", 6, 6)]
        [InlineData("<html><head ><title>", "head", 6, 7)]
        [InlineData("<html>< head><title>", "head", 6, 7)]

        [InlineData("<html><head>", "head", 6, 6)]
        [InlineData("<html><head >", "head", 6, 7)]
        [InlineData("<html>< head>", "head", 6, 7)]

        [InlineData("<head><title>", "head", 0, 6)]
        [InlineData("<head ><title>", "head", 0, 7)]
        [InlineData("< head><title>", "head", 0, 7)]

        [InlineData("<html>< head ><title>", "head", 6, 8)]
        [InlineData("<html>< head title='' anorther tag ><title>", "head", 6, 30)]

        [InlineData("<html><body><title>", "head", -1, 0)]
        [InlineData("<hea d><he ad><title>", "head", -1, 0)]

        [InlineData("<hea d><he ad><title>", "", -1, 0)]
        [InlineData("", "", -1, 0)]
        [InlineData("<hea<html>< head><title>", "head", 10, 7)]
        public void Test_Ordinal(string htmlContent, string searchTag, int foundIndex, int foundCount)
        {
            var matcher = new SimpleHtmlTagOpeningMatcher(Encoding.UTF8, StringComparison.OrdinalIgnoreCase);

            var (index, length) = matcher.FindIndex(htmlContent, searchTag);

            Assert.Equal(foundIndex, index);
            Assert.Equal(foundCount, length);
        }

        [Theory]
        [InlineData("<html><hEad><title>", "head", 6, 6)]
        [InlineData("<html><heaD ><title>", "heaD", 6, 7)]
        [InlineData("<html>< Head><title>", "hEad", 6, 7)]

        [InlineData("<html><heAd>", "head", 6, 6)]

        [InlineData("<html>< head ><title>", "head", 6, 8)]
        [InlineData("<html>< HEAD title='' anorther tag ><title>", "head", 6, 30)]
        public void Test_Ordinal_Ignore_Case(string htmlContent, string searchTag, int foundIndex, int foundCount)
        {
            var matcher = new SimpleHtmlTagOpeningMatcher(Encoding.UTF8, StringComparison.OrdinalIgnoreCase);

            var (index, length) = matcher.FindIndex(htmlContent, searchTag);

            Assert.Equal(foundIndex, index);
            Assert.Equal(foundCount, length);
        }
    }
}
