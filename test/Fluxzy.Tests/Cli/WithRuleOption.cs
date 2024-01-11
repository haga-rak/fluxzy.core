// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests.Cli.Scaffolding;
using Xunit;

// ReSharper disable MethodHasAsyncOverload

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOption : WithRuleOptionBase
    {
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

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://www.example.com");

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
