// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Writers;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class ExportCertificateInSslInfoIntegrationTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ExportCertificateInSslInfo_ViaProxy_WhenEnabled_PopulatesRemoteCertificatePem(bool useBouncyCastle)
        {
            // Arrange
            var setting = FluxzySetting.CreateLocalRandomPort()
                                       .SetExportCertificateInSslInfo(true);

            if (useBouncyCastle)
                setting.UseBouncyCastleSslEngine();

            var exchanges = new List<Exchange>();

            await using var proxy = new Proxy(setting);

            proxy.Writer.ExchangeUpdated += (_, args) =>
            {
                if (args.UpdateType == ArchiveUpdateType.AfterResponse)
                    exchanges.Add(args.Original);
            };

            var endpoints = proxy.Run();

            using var client = HttpClientUtility.CreateHttpClient(endpoints, setting);

            // Act
            var response = await client.GetAsync(TestConstants.Http2Host + "/");
            response.EnsureSuccessStatusCode();

            // Wait a bit for the exchange to be processed
            await Task.Delay(100);

            // Assert
            Assert.NotEmpty(exchanges);

            var exchange = exchanges.First();
            var sslInfo = exchange.Connection?.SslInfo;

            Assert.NotNull(sslInfo);
            Assert.NotNull(sslInfo.RemoteCertificatePem);
            Assert.Contains("-----BEGIN CERTIFICATE-----", sslInfo.RemoteCertificatePem);
            Assert.Contains("-----END CERTIFICATE-----", sslInfo.RemoteCertificatePem);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ExportCertificateInSslInfo_ViaProxy_WhenDisabled_RemoteCertificatePemIsNull(bool useBouncyCastle)
        {
            // Arrange
            var setting = FluxzySetting.CreateLocalRandomPort()
                                       .SetExportCertificateInSslInfo(false);

            if (useBouncyCastle)
                setting.UseBouncyCastleSslEngine();

            var exchanges = new List<Exchange>();

            await using var proxy = new Proxy(setting);

            proxy.Writer.ExchangeUpdated += (_, args) =>
            {
                if (args.UpdateType == ArchiveUpdateType.AfterResponse)
                    exchanges.Add(args.Original);
            };

            var endpoints = proxy.Run();

            using var client = HttpClientUtility.CreateHttpClient(endpoints, setting);

            // Act
            var response = await client.GetAsync(TestConstants.Http2Host + "/");
            response.EnsureSuccessStatusCode();

            // Wait a bit for the exchange to be processed
            await Task.Delay(100);

            // Assert
            Assert.NotEmpty(exchanges);

            var exchange = exchanges.First();
            var sslInfo = exchange.Connection?.SslInfo;

            Assert.NotNull(sslInfo);
            Assert.Null(sslInfo.RemoteCertificatePem);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ExportCertificateInSslInfo_ViaProxy_DefaultValue_RemoteCertificatePemIsNull(bool useBouncyCastle)
        {
            // Arrange - Don't set ExportCertificateInSslInfo (should default to false)
            var setting = FluxzySetting.CreateLocalRandomPort();

            if (useBouncyCastle)
                setting.UseBouncyCastleSslEngine();

            var exchanges = new List<Exchange>();

            await using var proxy = new Proxy(setting);

            proxy.Writer.ExchangeUpdated += (_, args) =>
            {
                if (args.UpdateType == ArchiveUpdateType.AfterResponse)
                    exchanges.Add(args.Original);
            };

            var endpoints = proxy.Run();

            using var client = HttpClientUtility.CreateHttpClient(endpoints, setting);

            // Act
            var response = await client.GetAsync(TestConstants.Http2Host + "/");
            response.EnsureSuccessStatusCode();

            // Wait a bit for the exchange to be processed
            await Task.Delay(100);

            // Assert
            Assert.NotEmpty(exchanges);

            var exchange = exchanges.First();
            var sslInfo = exchange.Connection?.SslInfo;

            Assert.NotNull(sslInfo);
            Assert.Null(sslInfo.RemoteCertificatePem);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ExportCertificateInSslInfo_ViaProxy_WhenEnabled_CertificatePemIsValidBase64(bool useBouncyCastle)
        {
            // Arrange
            var setting = FluxzySetting.CreateLocalRandomPort()
                                       .SetExportCertificateInSslInfo(true);

            if (useBouncyCastle)
                setting.UseBouncyCastleSslEngine();

            var exchanges = new List<Exchange>();

            await using var proxy = new Proxy(setting);

            proxy.Writer.ExchangeUpdated += (_, args) =>
            {
                if (args.UpdateType == ArchiveUpdateType.AfterResponse)
                    exchanges.Add(args.Original);
            };

            var endpoints = proxy.Run();

            using var client = HttpClientUtility.CreateHttpClient(endpoints, setting);

            // Act
            var response = await client.GetAsync(TestConstants.Http2Host + "/");
            response.EnsureSuccessStatusCode();

            // Wait a bit for the exchange to be processed
            await Task.Delay(100);

            // Assert
            Assert.NotEmpty(exchanges);

            var exchange = exchanges.First();
            var sslInfo = exchange.Connection?.SslInfo;

            Assert.NotNull(sslInfo);
            Assert.NotNull(sslInfo.RemoteCertificatePem);

            // Extract and validate base64 content
            var pemContent = sslInfo.RemoteCertificatePem;
            var startMarker = "-----BEGIN CERTIFICATE-----";
            var endMarker = "-----END CERTIFICATE-----";

            var startIndex = pemContent.IndexOf(startMarker) + startMarker.Length;
            var endIndex = pemContent.IndexOf(endMarker);
            var base64Content = pemContent.Substring(startIndex, endIndex - startIndex)
                                          .Replace("\r", "")
                                          .Replace("\n", "");

            var certBytes = Convert.FromBase64String(base64Content);
            Assert.NotEmpty(certBytes);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ExportCertificateInSslInfo_ViaProxy_WhenEnabled_OtherSslInfoPropertiesStillPopulated(bool useBouncyCastle)
        {
            // Arrange
            var setting = FluxzySetting.CreateLocalRandomPort()
                                       .SetExportCertificateInSslInfo(true);

            if (useBouncyCastle)
                setting.UseBouncyCastleSslEngine();

            var exchanges = new List<Exchange>();

            await using var proxy = new Proxy(setting);

            proxy.Writer.ExchangeUpdated += (_, args) =>
            {
                if (args.UpdateType == ArchiveUpdateType.AfterResponse)
                    exchanges.Add(args.Original);
            };

            var endpoints = proxy.Run();

            using var client = HttpClientUtility.CreateHttpClient(endpoints, setting);

            // Act
            var response = await client.GetAsync(TestConstants.Http2Host + "/");
            response.EnsureSuccessStatusCode();

            // Wait a bit for the exchange to be processed
            await Task.Delay(100);

            // Assert
            Assert.NotEmpty(exchanges);

            var exchange = exchanges.First();
            var sslInfo = exchange.Connection?.SslInfo;

            Assert.NotNull(sslInfo);
            Assert.NotNull(sslInfo.RemoteCertificateSubject);
            Assert.NotNull(sslInfo.RemoteCertificateIssuer);
            Assert.NotNull(sslInfo.NegotiatedApplicationProtocol);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ExportCertificateInSslInfo_ViaProxy_MultipleRequests_AllHaveCertificates(bool useBouncyCastle)
        {
            // Arrange
            var setting = FluxzySetting.CreateLocalRandomPort()
                                       .SetExportCertificateInSslInfo(true);

            if (useBouncyCastle)
                setting.UseBouncyCastleSslEngine();

            var exchanges = new List<Exchange>();

            await using var proxy = new Proxy(setting);

            proxy.Writer.ExchangeUpdated += (_, args) =>
            {
                if (args.UpdateType == ArchiveUpdateType.AfterResponse)
                    exchanges.Add(args.Original);
            };

            var endpoints = proxy.Run();

            using var client = HttpClientUtility.CreateHttpClient(endpoints, setting);

            // Act - Make multiple requests
            for (int i = 0; i < 3; i++)
            {
                var response = await client.GetAsync(TestConstants.Http2Host + "/");
                response.EnsureSuccessStatusCode();
            }

            // Wait a bit for the exchanges to be processed
            await Task.Delay(200);

            // Assert
            Assert.Equal(3, exchanges.Count);

            foreach (var exchange in exchanges)
            {
                var sslInfo = exchange.Connection?.SslInfo;
                Assert.NotNull(sslInfo);
                Assert.NotNull(sslInfo.RemoteCertificatePem);
                Assert.Contains("-----BEGIN CERTIFICATE-----", sslInfo.RemoteCertificatePem);
            }
        }
    }
}
