// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public abstract class WithRuleOptionGenericRequestFilters : WithRuleOptionBase
    {
        protected virtual HttpMethod Method { get; } = HttpMethod.Get;

        private string ActionAppend =
            @"
  action : 
    typeKind: AddResponseHeaderAction
    headerName: x-fluxzy-test
    headerValue: pass";

        protected abstract string YamlContent { get; }

        protected abstract void ConfigurePass(HttpRequestMessage requestMessage);

        protected abstract void ConfigureBlock(HttpRequestMessage requestMessage);

        [Fact]
        public async Task Validate_Pass()
        {
            // Arrange
            var yamlContent = YamlContent + ActionAppend;

            var requestMessage = new HttpRequestMessage(Method,
                $"{TestConstants.Http2Host}/global-health-check");

            ConfigurePass(requestMessage);

            // Act 
            using var response = await Exec(yamlContent, requestMessage);

            var stream = await response.Content.ReadAsStreamAsync();

            await stream.DrainAsync();

            response.Headers.TryGetValues("x-fluxzy-test", out var values);

            Assert.NotNull(values);
            Assert.Equal("pass", values.First());
        }

        [Fact]
        public async Task Validate_Block()
        {
            // Arrange
            var yamlContent = YamlContent + ActionAppend;

            var requestMessage = new HttpRequestMessage(Method,
                $"{TestConstants.Http2Host}/global-health-check");

            ConfigureBlock(requestMessage);

            // Act 
            using var response = await Exec(yamlContent, requestMessage);

            var stream = await response.Content.ReadAsStreamAsync();

            await stream.DrainAsync();

            var res = response.Headers.TryGetValues("x-fluxzy-test", out var values);

            Assert.False(res);
        }
    }
}
