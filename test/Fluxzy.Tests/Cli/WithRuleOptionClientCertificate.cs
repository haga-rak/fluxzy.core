// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests._Files;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WithRuleOptionClientCertificate : WithRuleOptionBase
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public async Task Validate_ClientCertificate_Fluxzy_Server(bool forceH11, bool useBouncyCastle)
        {
            // Arrange 
            var requestMessage =
                new HttpRequestMessage(HttpMethod.Get, $"{TestConstants.GetHost("http2")}/certificate");

            requestMessage.Headers.Add("X-Test-Header-256", "That value");

            File.WriteAllBytes($"cc.pfx", StorageContext.client_cert);

            var yamlContent = $"""
                              rules:
                                - filter:
                                    typeKind: AnyFilter
                                  action :
                                    typeKind: SetClientCertificateAction
                                    clientCertificate:
                                      pkcs12File: cc.pfx
                                      pkcs12Password: {CertificateContext.DefaultPassword}
                                      retrieveMode: FromPkcs12
                              """;

            var yamlContentForceHttp11 = $"""
                                         rules:
                                           - filter:
                                               typeKind: AnyFilter
                                             action :
                                               typeKind: SetClientCertificateAction
                                               clientCertificate:
                                                 pkcs12File: cc.pfx
                                                 pkcs12Password: {CertificateContext.DefaultPassword}
                                                 retrieveMode: FromPkcs12
                                           - filter:
                                               typeKind: AnyFilter
                                             action :
                                               typeKind: ForceHttp11Action
                                         """;

            using var response = await Exec(forceH11 ? yamlContentForceHttp11 : yamlContent, 
                requestMessage, useBouncyCastle: useBouncyCastle);
            
            var thumbPrint = await response.Content.ReadAsStringAsync();
            var expectedThumbPrint = "960b00317d47d0d52d04a3a03b045e96bf3be3a3";

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedThumbPrint, thumbPrint, StringComparer.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public async Task Validate_ClientCertificate_Cryptomix(bool forceH11, bool useBouncyCastle)
        {
            var requestMessage =
                new HttpRequestMessage(HttpMethod.Get, $"https://certauth.cryptomix.com/json/");

            requestMessage.Headers.Add("X-Test-Header-256", "That value");
            
            File.WriteAllBytes("cc2.pfx", StorageContext.client_cert);
            File.WriteAllBytes("cc.pfx", StorageContext.client_cert);

            var yamlContent = $"""
                              rules:
                                - filter:
                                    typeKind: AnyFilter
                                  action :
                                    typeKind: SetClientCertificateAction
                                    clientCertificate:
                                      pkcs12File: cc2.pfx
                                      pkcs12Password: {CertificateContext.DefaultPassword}
                                      retrieveMode: FromPkcs12
                              """;

            var yamlContentForceHttp11 = $"""
                                         rules:
                                           - filter:
                                               typeKind: AnyFilter
                                             action :
                                               typeKind: SetClientCertificateAction
                                               clientCertificate:
                                                 pkcs12File: cc.pfx
                                                 pkcs12Password: {CertificateContext.DefaultPassword}
                                                 retrieveMode: FromPkcs12
                                           - filter:
                                               typeKind: AnyFilter
                                             action :
                                               typeKind: ForceHttp11Action
                                         """;
            
            using var response = await Exec(forceH11 ? yamlContentForceHttp11 : yamlContent, 
                requestMessage, useBouncyCastle: useBouncyCastle);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
