// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests._Files;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Tests.Cli.Scaffolding;
using Xunit;

// ReSharper disable MethodHasAsyncOverload

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOption : WithRuleOptionBase
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Validate_ClientCertificate(bool forceH11)
        {
            // Arrange 
            var requestMessage =
                new HttpRequestMessage(HttpMethod.Get, $"{TestConstants.GetHost("http2")}/certificate");

            requestMessage.Headers.Add("X-Test-Header-256", "That value");

            File.WriteAllBytes($"{nameof(Validate_ClientCertificate)}.pfx", StorageContext.client_cert);

            var yamlContent = """
                rules:
                  - filter: 
                      typeKind: AnyFilter        
                    action : 
                      typeKind: SetClientCertificateAction
                      clientCertificate: 
                        pkcs12File: cc.pfx
                        pkcs12Password: Multipass85/
                        retrieveMode: FromPkcs12
                """;

            var yamlContentForceHttp11 = """
                rules:
                  - filter: 
                      typeKind: AnyFilter        
                    action : 
                      typeKind: SetClientCertificateAction
                      clientCertificate: 
                        pkcs12File: cc.pfx
                        pkcs12Password: Multipass85/
                        retrieveMode: FromPkcs12 
                  - filter: 
                      typeKind: AnyFilter        
                    action : 
                      typeKind: ForceHttp11Action
                """;

            using var response = await Exec(forceH11 ? yamlContentForceHttp11 : yamlContent, requestMessage);
            
            var thumbPrint = await response.Content.ReadAsStringAsync();
            var expectedThumbPrint = "960b00317d47d0d52d04a3a03b045e96bf3be3a3";

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedThumbPrint, thumbPrint, StringComparer.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Validate_ClientCertificate_2(bool forceH11)
        {
            var requestMessage =
                new HttpRequestMessage(HttpMethod.Get, $"https://certauth.cryptomix.com/json/");

            requestMessage.Headers.Add("X-Test-Header-256", "That value");
            
            File.WriteAllBytes("cc.pfx", StorageContext.client_cert);

            var yamlContent = """
                rules:
                  - filter: 
                      typeKind: AnyFilter        
                    action : 
                      typeKind: SetClientCertificateAction
                      clientCertificate: 
                        pkcs12File: cc.pfx
                        pkcs12Password: Multipass85/
                        retrieveMode: FromPkcs12
                """;

            var yamlContentForceHttp11 = """
                rules:
                  - filter: 
                      typeKind: AnyFilter        
                    action : 
                      typeKind: SetClientCertificateAction
                      clientCertificate: 
                        pkcs12File: cc.pfx
                        pkcs12Password: Multipass85/
                        retrieveMode: FromPkcs12 
                  - filter: 
                      typeKind: AnyFilter        
                    action : 
                      typeKind: ForceHttp11Action
                """;
            
            using var response = await Exec(forceH11 ? yamlContentForceHttp11 : yamlContent, requestMessage);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
