// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Utils;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Util
{
    public class StringHelperTests
    {
        [Theory]
        [InlineData("HelloWorld", "helloWorld")]
        [InlineData("helloWorld", "helloWorld")]
        [InlineData("hello", "hello")]
        [InlineData("h", "h")]
        [InlineData("", "")]
        public void ToCamelCase(string input, string expected)
        {
            var result = input.ToCamelCase();

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("helloWorld", "HelloWorld.")]
        [InlineData("HelloWorld", "HelloWorld.")]
        [InlineData("hello", "Hello.")]
        [InlineData("h", "h")]
        [InlineData("", "")]
        public void AddTrailingDotAndUpperCaseFirstChar(string input, string expected)
        {
            var result = input.AddTrailingDotAndUpperCaseFirstChar();

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("hello\"World", "hello\\\"World")]
        [InlineData("helloWorld", "helloWorld")]
        [InlineData("hello", "hello")]
        [InlineData("h", "h")]
        [InlineData("", "")]
        public void EscapeDoubleQuote(string input, string expected)
        {
            var result = input.EscapeDoubleQuote();

            Assert.Equal(expected, result);
        }
    }
}
