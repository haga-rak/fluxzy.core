// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Tests.Cli.Scaffolding;
using Xunit;

// ReSharper disable MethodHasAsyncOverload

namespace Fluxzy.Tests.Cli
{
    [Collection("WithRuleOption [Run last]")]
    public class WithRuleOption : WithRuleOptionBase
    {
        [Theory]
        [InlineData("fluxzy-rule.yml", true)]
        [InlineData("fluxzy-rule.yaml", true)]
        [InlineData("invalid.yaml", false)]
        public async Task Validate_DefaultFileName(string filename, bool success)
        {
            // Arrange 
            var commandLine = "start -l 127.0.0.1/0";
            var ruleFile = filename;

            var yamlContent = """
                rules:
                - filter:
                    typeKind: absoluteUriFilter
                    pattern: https://sandbox.fluxzy.io/this-can-be-a-real-directory
                  actions:
                  - typeKind: MockedResponseAction
                    response:
                      statusCode: 202
                      body:
                        origin: FromString
                        type: text
                        text: 'OK computer'
                """;

            File.WriteAllText(ruleFile, yamlContent);

            var commandLineHost = new FluxzyCommandLineHost(commandLine);

            await using var fluxzyInstance = await commandLineHost.Run();
            using var proxiedHttpClient = new ProxiedHttpClient(fluxzyInstance.ListenPort);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://sandbox.fluxzy.io/this-can-be-a-real-directory");

            requestMessage.Headers.Add("User-Agent", "Unit test");

            // Act 
            using var response = await proxiedHttpClient.Client.SendAsync(requestMessage);

            var fullResponse = await response.Content.ReadAsStringAsync();

            if (success) {
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
                Assert.Equal("OK computer", fullResponse);
            }
            else {
                Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Fact]
        public async Task Validate_UpdateRequestHeaderAction()
        {
            // Arrange 
            var commandLine = "start -l 127.0.0.1/0";
            var ruleFile = $"{nameof(Validate_UpdateRequestHeaderAction)}.yml";

            var yamlContent = """
                rules:
                - filter: 
                    typeKind: AnyFilter        
                  action : 
                    typeKind: UpdateRequestHeaderAction
                    headerName: user-agent
                    # previous reference the original value of the user-agent header
                    headerValue: "{{previous}} - Relayed by fluxzy"
                    addIfMissing: true
                """;

            File.WriteAllText(ruleFile, yamlContent);

            commandLine += $" -r {ruleFile}";

            var commandLineHost = new FluxzyCommandLineHost(commandLine);

            await using var fluxzyInstance = await commandLineHost.Run();
            using var proxiedHttpClient = new ProxiedHttpClient(fluxzyInstance.ListenPort);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, TestConstants.TestDomain);

            requestMessage.Headers.Add("User-Agent", "Unit test");

            // Act 
            using var response = await proxiedHttpClient.Client.SendAsync(requestMessage);

            await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        }

        [Fact]
        public async Task Validate_IpIngress()
        {
            // Arrange
            var yamlContent = """
                rules:
                - filter: 
                    typeKind: IpIngressFilter  
                    pattern: 127.0.0.1
                    operation: exact
                  action : 
                    typeKind: UpdateResponseHeaderAction
                    headerName: fromLocalHost
                    headerValue: "Relayed by fluxzy"
                    addIfMissing: true
                """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, TestConstants.TestDomain);

            // Act 
            using var response = await Exec(yamlContent, requestMessage);


            var hasHeader = response.Headers.TryGetValues("fromLocalHost", out var headerValues);
            var headerValue = headerValues?.FirstOrDefault();

            Assert.True(hasHeader);
            Assert.Equal("Relayed by fluxzy", headerValue);

        }
    }
}
