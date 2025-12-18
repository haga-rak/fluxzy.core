// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Core;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cli
{
    public class WitRuleOptionServerCertificateAction : WithRuleOptionBase
    {
        [Fact]
        public async Task Validate()
        {
            var rootCertificate = new CertificateBuilder(new CertificateBuilderOptions("TEST_FR"));
            var selfSigned = rootCertificate.CreateSelfSigned();
            var tempFile = GetTempFile(); 
            
            await File.WriteAllBytesAsync(tempFile.FullName, selfSigned);

            var certificateProvider = new CertificateProvider(Certificate.LoadFromPkcs12(tempFile.FullName),
                new InMemoryCertificateCache());

            var certificateData = certificateProvider.GetCertificateBytes("sandbox.fluxzy.io");
            var serverCertificateFile = GetTempFile();
            await File.WriteAllBytesAsync(serverCertificateFile.FullName, certificateData);

            // Arrange
            var yamlContent = $"""
                               rules:
                               - filter:
                                   typeKind: HostFilter
                                   pattern: sandbox.fluxzy.io
                                   operation: contains
                                 action :
                                   typeKind: useCertificateAction
                                   serverCertificate:
                                      pkcs12File: {serverCertificateFile.FullName}
                                      retrieveMode: FromPkcs12
                               """;

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                TestConstants.TestDomain);


            // Act
            using var response = await Exec(yamlContent, requestMessage, allowAutoRedirect: false);

            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(Client);
            Assert.NotNull(Client.ServerCertificate);
            Assert.Equal("CN=TEST_FR", Client!.ServerCertificateIssuer);
        }
    }
}
