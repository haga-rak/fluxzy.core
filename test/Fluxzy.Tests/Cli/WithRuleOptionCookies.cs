// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionCookies : WithRuleOptionBase
    {
        [Fact]
        public async Task ValidateSetRequestCookie()
        {
            // Arrange
            var yamlContent = """
                rules:
                - filter: 
                    typeKind: HostFilter  
                    pattern: example.com
                    operation: endsWith
                  action : 
                    typeKind: setRequestCookieAction
                    name: coco
                    value: lolo
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://www.example.com");

            // Act 
            using var response = await Exec(yamlContent, requestMessage);


            var hasHeader = response.Headers.TryGetValues("fromLocalHost", out var headerValues);
            var headerValue = headerValues?.FirstOrDefault();

            Assert.True(hasHeader);
            Assert.Equal("Relayed by fluxzy", headerValue);

        }
    }
}
