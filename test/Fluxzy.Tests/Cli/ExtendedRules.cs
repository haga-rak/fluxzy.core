// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class ExtendedRules
    {
        [Fact]
        public async Task Exec_Filter_True()
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Delete,
                $"{TestConstants.GetHost("http2")}/global-health-check");

            var rule = """
rules:
- filter:
    typeKind: exec
    filename: true
  actions: 
  - typeKind: applyCommentAction
    comment: filtered

""";
            var (exchange, _, _) = await RequestHelper.DirectRequest(requestMessage, rule);

            Assert.NotNull(exchange.ResponseHeader);
            Assert.Equal("filtered", exchange.Comment);
        }

        [Fact]
        public async Task Exec_Filter_False()
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Delete,
                $"{TestConstants.GetHost("http2")}/global-health-check");

            var rule = """
rules:
- filter:
    typeKind: exec
    filename: false
  actions: 
  - typeKind: applyCommentAction
    comment: filtered

""";
            var (exchange, _, _) = await RequestHelper.DirectRequest(requestMessage, rule);

            Assert.NotNull(exchange.ResponseHeader);
            Assert.NotEqual("filtered", exchange.Comment);
        }
    }
}
