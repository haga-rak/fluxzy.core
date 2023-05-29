// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Rules;
using Xunit;

namespace Fluxzy.Tests.Variables
{
    public class VariableContextTests
    {
        [Fact]
        public void Push_And_Get()
        {
            VariableContext holder = new();

            holder.Set("test", "yoyo");

            Assert.True(holder.TryGet("test", out var value));
            Assert.Equal("yoyo", value);
        }

        [Fact]
        public void Update_Should_Be_Made()
        {
            VariableContext holder = new();

            holder.Set("test", "yoyo");

            var result = holder.EvaluateVariable("Hello ${test}!", null);

            Assert.Equal("Hello yoyo!", result);
        }

        [Theory]
        [InlineData("Hello ${{test}!")]
        [InlineData("Hello ${{test}}!")]
        [InlineData("")]
        [InlineData(" ")]
        public void ShouldNotMatch(string input)
        {
            VariableContext holder = new();

            holder.Set("test", "yoyo");

            var result = holder.EvaluateVariable(input, null);

            Assert.Equal(input, result);
            Assert.Same(input, result);
        }
    }
}
