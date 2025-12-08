// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Clients.DotNetBridge;
using Fluxzy.Core;
using Fluxzy.Writers;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Handlers
{
    public class ExportCertificateInSslInfoTests
    {
        [Theory]
        [InlineData(SslProvider.OsDefault)]
        [InlineData(SslProvider.BouncyCastle)]
        public async Task ExportCertificateInSslInfo_WhenEnabled_PopulatesRemoteCertificatePem(SslProvider sslProvider)
        {
            // Arrange
            await using var tcpProvider = ITcpConnectionProvider.Default;

            using var handler = new FluxzyDefaultHandler(sslProvider, tcpProvider, new EventOnlyArchiveWriter());

            handler.ConfigureContext = (exchangeContext) => {
                exchangeContext.AdvancedTlsSettings.ExportCertificateInSslInfo = true;
            };

            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://www.google.com/");

            // Act
            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            // Assert
            var fluxzyResponse = Assert.IsType<FluxzyHttpResponseMessage>(response);
            var sslInfo = fluxzyResponse.Exchange.Connection?.SslInfo;

            Assert.NotNull(sslInfo);
            Assert.NotNull(sslInfo.RemoteCertificatePem);
            Assert.Contains("-----BEGIN CERTIFICATE-----", sslInfo.RemoteCertificatePem);
            Assert.Contains("-----END CERTIFICATE-----", sslInfo.RemoteCertificatePem);
        }

        [Theory]
        [InlineData(SslProvider.OsDefault)]
        [InlineData(SslProvider.BouncyCastle)]
        public async Task ExportCertificateInSslInfo_WhenDisabled_RemoteCertificatePemIsNull(SslProvider sslProvider)
        {
            // Arrange
            await using var tcpProvider = ITcpConnectionProvider.Default;

            using var handler = new FluxzyDefaultHandler(sslProvider, tcpProvider, new EventOnlyArchiveWriter());

            handler.ConfigureContext = (exchangeContext) => {
                exchangeContext.AdvancedTlsSettings.ExportCertificateInSslInfo = false;
            };

            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://www.google.com/");

            // Act
            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            // Assert
            var fluxzyResponse = Assert.IsType<FluxzyHttpResponseMessage>(response);
            var sslInfo = fluxzyResponse.Exchange.Connection?.SslInfo;

            Assert.NotNull(sslInfo);
            Assert.Null(sslInfo.RemoteCertificatePem);
        }

        [Theory]
        [InlineData(SslProvider.OsDefault)]
        [InlineData(SslProvider.BouncyCastle)]
        public async Task ExportCertificateInSslInfo_DefaultValue_RemoteCertificatePemIsNull(SslProvider sslProvider)
        {
            // Arrange - Do not configure ExportCertificateInSslInfo (should default to false)
            await using var tcpProvider = ITcpConnectionProvider.Default;

            using var handler = new FluxzyDefaultHandler(sslProvider, tcpProvider, new EventOnlyArchiveWriter());

            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://www.google.com/");

            // Act
            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            // Assert
            var fluxzyResponse = Assert.IsType<FluxzyHttpResponseMessage>(response);
            var sslInfo = fluxzyResponse.Exchange.Connection?.SslInfo;

            Assert.NotNull(sslInfo);
            Assert.Null(sslInfo.RemoteCertificatePem);
        }

        [Theory]
        [InlineData(SslProvider.OsDefault)]
        [InlineData(SslProvider.BouncyCastle)]
        public async Task ExportCertificateInSslInfo_WhenEnabled_CertificatePemIsValidBase64(SslProvider sslProvider)
        {
            // Arrange
            await using var tcpProvider = ITcpConnectionProvider.Default;

            using var handler = new FluxzyDefaultHandler(sslProvider, tcpProvider, new EventOnlyArchiveWriter());

            handler.ConfigureContext = (exchangeContext) => {
                exchangeContext.AdvancedTlsSettings.ExportCertificateInSslInfo = true;
            };

            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://www.google.com/");

            // Act
            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            // Assert
            var fluxzyResponse = Assert.IsType<FluxzyHttpResponseMessage>(response);
            var sslInfo = fluxzyResponse.Exchange.Connection?.SslInfo;

            Assert.NotNull(sslInfo);
            Assert.NotNull(sslInfo.RemoteCertificatePem);

            // Extract the base64 content between BEGIN and END markers
            var pemContent = sslInfo.RemoteCertificatePem;
            var startMarker = "-----BEGIN CERTIFICATE-----";
            var endMarker = "-----END CERTIFICATE-----";

            var startIndex = pemContent.IndexOf(startMarker) + startMarker.Length;
            var endIndex = pemContent.IndexOf(endMarker);
            var base64Content = pemContent.Substring(startIndex, endIndex - startIndex)
                                          .Replace("\r", "")
                                          .Replace("\n", "");

            // Verify the base64 content is valid
            var certBytes = Convert.FromBase64String(base64Content);
            Assert.NotEmpty(certBytes);
        }

        [Theory]
        [InlineData(SslProvider.OsDefault)]
        [InlineData(SslProvider.BouncyCastle)]
        public async Task ExportCertificateInSslInfo_WhenEnabled_SslInfoStillHasOtherProperties(SslProvider sslProvider)
        {
            // Arrange
            await using var tcpProvider = ITcpConnectionProvider.Default;

            using var handler = new FluxzyDefaultHandler(sslProvider, tcpProvider, new EventOnlyArchiveWriter());

            handler.ConfigureContext = (exchangeContext) => {
                exchangeContext.AdvancedTlsSettings.ExportCertificateInSslInfo = true;
            };

            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://www.google.com/");

            // Act
            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            // Assert - Verify other SSL info properties are still populated
            var fluxzyResponse = Assert.IsType<FluxzyHttpResponseMessage>(response);
            var sslInfo = fluxzyResponse.Exchange.Connection?.SslInfo;

            Assert.NotNull(sslInfo);
            Assert.NotNull(sslInfo.RemoteCertificateSubject);
            Assert.NotNull(sslInfo.RemoteCertificateIssuer);
            Assert.NotNull(sslInfo.NegotiatedApplicationProtocol);
        }
    }
}
