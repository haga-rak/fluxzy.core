// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Tests.Common;
using Xunit;

namespace Fluxzy.Tests.Rules
{
    public class GlobalFilterTests : FilterTestTemplate
    {
        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]
        public async Task CheckPass_HasCookieOnRequestFilter(string host)
        {
            var filter = new HasCookieOnRequestFilter("test", "1", StringSelectorOperation.Exact);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            requestMessage.Headers.Add("Cookie", "dummyCookieNoMatchingValue=zezef");
            requestMessage.Headers.Add("Cookie", "test=1");

            var result = await CheckPass(requestMessage, filter);

            Assert.True(result);
        }

        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]
        public async Task CheckSkipped_HasCookieOnRequestFilter(string host)
        {
            var filter = new HasCookieOnRequestFilter("test", "2", StringSelectorOperation.Exact);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            requestMessage.Headers.Add("Cookie", "test=1");

            var result = await CheckPass(requestMessage, filter);

            Assert.False(result);
        }
    }
}
