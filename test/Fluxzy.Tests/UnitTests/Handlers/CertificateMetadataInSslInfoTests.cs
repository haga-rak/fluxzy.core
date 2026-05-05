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
    public class CertificateMetadataInSslInfoTests
    {
        [Theory]
        [InlineData(SslProvider.OsDefault)]
        [InlineData(SslProvider.BouncyCastle)]
        public async Task RemoteCertificate_ValidityDates_AreWithinPlausibleWindow(SslProvider sslProvider)
        {
            var sslInfo = await GetSslInfoForGoogle(sslProvider);

            Assert.NotNull(sslInfo);
            Assert.NotNull(sslInfo.RemoteCertificateNotBefore);
            Assert.NotNull(sslInfo.RemoteCertificateNotAfter);

            var now = DateTime.UtcNow;
            Assert.True(sslInfo.RemoteCertificateNotBefore <= now,
                $"NotBefore {sslInfo.RemoteCertificateNotBefore} should be in the past");
            Assert.True(sslInfo.RemoteCertificateNotAfter >= now,
                $"NotAfter {sslInfo.RemoteCertificateNotAfter} should be in the future");
            Assert.True(sslInfo.RemoteCertificateNotBefore < sslInfo.RemoteCertificateNotAfter);
        }

        [Theory]
        [InlineData(SslProvider.OsDefault)]
        [InlineData(SslProvider.BouncyCastle)]
        public async Task RemoteCertificate_SerialNumber_IsUppercaseHex(SslProvider sslProvider)
        {
            var sslInfo = await GetSslInfoForGoogle(sslProvider);

            Assert.NotNull(sslInfo);
            Assert.NotNull(sslInfo.RemoteCertificateSerialNumber);

            var serial = sslInfo.RemoteCertificateSerialNumber!;
            Assert.NotEmpty(serial);
            Assert.All(serial, c => Assert.True(
                (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F'),
                $"Character '{c}' is not uppercase hex"));
        }

        [Fact]
        public async Task RemoteCertificate_SerialNumber_IsConsistentAcrossProviders()
        {
            // Hit both providers back-to-back to maximise odds of landing on the same cert.
            // If the host rotates certs between calls this will flake; the value of the test
            // is catching a wrong-bytes bug in the BC path, not load-balancer behaviour.
            var fromOs = await GetSslInfoForGoogle(SslProvider.OsDefault);
            var fromBc = await GetSslInfoForGoogle(SslProvider.BouncyCastle);

            Assert.NotNull(fromOs?.RemoteCertificateSerialNumber);
            Assert.NotNull(fromBc?.RemoteCertificateSerialNumber);

            // Same subject => same cert => same serial.
            if (string.Equals(fromOs.RemoteCertificateSubject, fromBc.RemoteCertificateSubject,
                    StringComparison.Ordinal))
            {
                Assert.Equal(fromOs.RemoteCertificateSerialNumber,
                    fromBc.RemoteCertificateSerialNumber);
            }
        }

        [Theory]
        [InlineData(SslProvider.OsDefault)]
        [InlineData(SslProvider.BouncyCastle)]
        public async Task LocalCertificate_NotPresented_FieldsAreNull(SslProvider sslProvider)
        {
            // No client cert configured for these tests => local fields stay null.
            var sslInfo = await GetSslInfoForGoogle(sslProvider);

            Assert.NotNull(sslInfo);
            Assert.Null(sslInfo.LocalCertificateNotBefore);
            Assert.Null(sslInfo.LocalCertificateNotAfter);
            Assert.Null(sslInfo.LocalCertificateSerialNumber);
        }

        private static async Task<SslInfo?> GetSslInfoForGoogle(SslProvider sslProvider)
        {
            await using var tcpProvider = ITcpConnectionProvider.Default;

            using var handler = new FluxzyDefaultHandler(sslProvider, tcpProvider, new EventOnlyArchiveWriter());

            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://www.google.com/");

            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var fluxzyResponse = Assert.IsType<FluxzyHttpResponseMessage>(response);
            return fluxzyResponse.Exchange.Connection?.SslInfo;
        }
    }
}
