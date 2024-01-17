// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Rules
{
    public class FilterTests : FilterTestTemplate
    {
        [Theory]
        [MemberData(nameof(TestConstants.GetHosts), MemberType = typeof(TestConstants))]
        public async Task Validate_NoFilter(string host)
        {
            var filter = new NoFilter();

            _ = filter.GetExamples();

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            var result = await CheckPass(requestMessage, filter);

            Assert.False(result);
        }

        [Theory]
        [MemberData(nameof(TestConstants.GetHosts), MemberType = typeof(TestConstants))]
        public async Task Validate_IpIngressFilter(string host)
        {
            var filter = new IpIngressFilter(".*", StringSelectorOperation.Regex);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            var result = await CheckPass(requestMessage, filter);

            Assert.True(result);
        }
        
        [Theory]
        [MemberData(nameof(TestConstants.GetHosts), MemberType = typeof(TestConstants))]
        public async Task Validate_IpIngressFilter_Fail(string host)
        {
            var filter = new IpIngressFilter("IMpossible", StringSelectorOperation.Regex);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            var result = await CheckPass(requestMessage, filter);

            Assert.False(result);
        }
        
        [Theory]
        [MemberData(nameof(TestConstants.GetHosts), MemberType = typeof(TestConstants))]
        public async Task Validate_TagContainsFilter(string host)
        {
            var tag = new Tag(Guid.Parse("0313F012-2F97-48CA-B7B8-63710056A922"), "test");

            var filter = new TagContainsFilter(tag);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            var result = await CheckPass(requestMessage, filter,
                settings => {
                    settings.AddAlterationRules(
                        new ApplyTagAction() {
                            Tag = new Tag(tag.Identifier, tag.Value)
                        },
                        AnyFilter.Default);
                });

            Assert.True(result);
        }

        [Theory]
        [MemberData(nameof(TestConstants.GetHosts), MemberType = typeof(TestConstants))]
        public async Task Validate_AgentLabelFilter(string host)
        {
            var filter = new AgentLabelFilter("chrome");

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check") {
            };

            requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            var result = await CheckPass(requestMessage, filter);

            Assert.True(result);
        }

        [Theory]
        [MemberData(nameof(TestConstants.GetHosts), MemberType = typeof(TestConstants))]
        public async Task Validate_AgentLabelFilter_Fail(string host)
        {
            var filter = new AgentLabelFilter("safari");

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check") {
            };

            requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            var result = await CheckPass(requestMessage, filter);

            Assert.False(result);
        }
    }
}
