// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionSetUserAgentAction : WithRuleOptionBase
    {

        [Theory]
        [MemberData(nameof(GetExpectedValues))]
        
        public async Task Validate(string name, string ? expectedValue)
        {
            // Arrange
            var yamlContent = $"""
                              rules:
                              - filter:
                                  typeKind: anyFilter
                                actions :
                                  - typeKind: userAgentAction
                                    name: {name}
                              """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{TestConstants.Http11Host}/global-health-check");

            // Act
            using var response = await Exec(yamlContent, requestMessage, allowAutoRedirect: false);

            var checkResult = await response.GetCheckResult();

            var userAgentHeader = 
                checkResult.Headers?
                           .SingleOrDefault(h => h.Name.Equals("user-agent", StringComparison.OrdinalIgnoreCase));

            if (expectedValue != null) {

                Assert.NotNull(userAgentHeader);
                Assert.NotNull(userAgentHeader.Value);

                Assert.Equal(expectedValue, userAgentHeader.Value);
            }
            else
            {
                Assert.Null(userAgentHeader);
            }
        }

        public static IEnumerable<object?[]> GetExpectedValues()
        {
            List<object?[]> result = new List<object?[]>();
            
            foreach (var (name, userAgent) in UserAgentActionMapping.Default.Map) {

                result.Add(new object[] { name, userAgent });
            }
            
            result.Add(new object?[] { "not-a-valid-entry", null });
            result.Add(new object?[] { null, null });

            return result; 
        }
    }
}
