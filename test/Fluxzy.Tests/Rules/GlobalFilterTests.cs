// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Test multiple filter
        /// </summary>
        /// <param name="host"></param>
        /// <param name="genericFilterInfo"></param>
        /// <returns></returns>
        [Theory]
        [MemberData(nameof(CheckPass_Generic_ArgBuilder))]
        public async Task CheckPass_Generic(string host, CheckPassGenericFilterInfo genericFilterInfo)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{host}/global-health-check");

            genericFilterInfo.AlterAction(requestMessage);

            var result = await CheckPass(requestMessage, genericFilterInfo.Filter);

            Assert.Equal(genericFilterInfo.ShouldPass, result);
        }

        public static IEnumerable<object[]> CheckPass_Generic_ArgBuilder()
        {
            var testedHosts = new string[] { TestConstants.Http11Host, TestConstants.Http2Host };
            
            var checkPassGenericFilterInfos = new List<CheckPassGenericFilterInfo>() {
                new(new HasAnyCookieOnRequestFilter(), r => r.Headers.Add("Cookie", "test=1"), true),
                new(new HasAnyCookieOnRequestFilter(), r =>  {}, false),
                new(new GetFilter(), r => r.Method = HttpMethod.Get, true),
                new(new GetFilter(), r => r.Method = HttpMethod.Delete, false),
                new(new FullUrlFilter("smartizy.com"), r => {}, true),
                new(new FullUrlFilter("smartzzizy.com"), r => {}, false),
            };

            foreach (var host in testedHosts) {
                foreach (var checkPassGenericFilterInfo in checkPassGenericFilterInfos) {

                    yield return new object[] { host, checkPassGenericFilterInfo };
                }
            }
        }
    }


    public class CheckPassGenericFilterInfo
    {
        public CheckPassGenericFilterInfo(Filter filter, Action<HttpRequestMessage> alterAction, bool shouldPass)
        {
            Filter = filter;
            AlterAction = alterAction;
            ShouldPass = shouldPass;
        }

        public Filter Filter { get; }

        public Action<HttpRequestMessage> AlterAction { get; }

        public bool ShouldPass { get; }
    }
}
