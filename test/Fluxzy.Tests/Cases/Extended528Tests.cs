// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class Extended528Tests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Validate_Expired_Ssl(bool useBouncyCastle)
        {
            var response = await Run("https://expired.badssl.com/", useBouncyCastle);

            Assert.Equal(528, (int) response.StatusCode);
            AssertNetworkErrorCode(response, NetworkErrorCodes.TlsCertExpired);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Validate_Wrong_Host(bool useBouncyCastle)
        {
            var response = await Run("https://wrong.host.badssl.com/", useBouncyCastle);

            Assert.Equal(528, (int) response.StatusCode);
            AssertNetworkErrorCode(response, NetworkErrorCodes.TlsCertHostnameMismatch);
        }

        // Trust-chain validation is OS-stack only: BouncyCastle's FluxzyTlsAuthentication
        // currently validates dates and hostname but does not verify the issuance chain,
        // so untrusted-root / self-signed scenarios go through unblocked under BC.
        [Fact]
        public async Task Validate_Untrusted_Root_Os()
        {
            var response = await Run("https://untrusted-root.badssl.com/", useBouncyCastle: false);

            Assert.Equal(528, (int) response.StatusCode);
            AssertNetworkErrorCode(response, NetworkErrorCodes.TlsCertUntrusted);
        }

        [Fact]
        public async Task Validate_Self_Signed_Os()
        {
            var response = await Run("https://self-signed.badssl.com/", useBouncyCastle: false);

            Assert.Equal(528, (int) response.StatusCode);
            AssertNetworkErrorCode(response, NetworkErrorCodes.TlsCertUntrusted);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Validate_Connection_Refused(bool useBouncyCastle)
        {
            // 127.0.0.1:1 is virtually guaranteed to refuse — nothing listens on TCP/1.
            var response = await Run("http://127.0.0.1:1/", useBouncyCastle);

            Assert.Equal(528, (int) response.StatusCode);
            AssertNetworkErrorCode(response, NetworkErrorCodes.ConnectionRefused);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Validate_Dns_NotFound(bool useBouncyCastle)
        {
            // .invalid TLD is reserved by RFC 6761 to never resolve.
            var response = await Run(
                "http://this.host.does.not.exist.fluxzy.invalid/", useBouncyCastle);

            Assert.Equal(528, (int) response.StatusCode);
            AssertNetworkErrorCode(response, NetworkErrorCodes.DnsNotFound);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Validate_With_Ip_Address(bool useBouncyCastle)
        {
            var response = await Run("https://1.1.1.1", useBouncyCastle);

            Assert.NotEqual(528, (int) response.StatusCode);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Validate_Abrupt_Close(bool useBouncyCastle)
        {
            // Server sends an RST. Different stacks/OSes report this as either
            // ConnectionReset or ConnectionAborted — either is a correct surfacing.
            await using var server = MisbehavingTcpServer.Start(MisbehaveMode.AbruptClose);
            var response = await Run($"http://127.0.0.1:{server.Port}/", useBouncyCastle);

            Assert.Equal(528, (int) response.StatusCode);
            AssertNetworkErrorCodeOneOf(response,
                NetworkErrorCodes.ConnectionReset,
                NetworkErrorCodes.ConnectionAborted);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Validate_Tls_Handshake_Failure(bool useBouncyCastle)
        {
            // Server replies with a TLS fatal alert (handshake_failure) so the proxy's
            // upstream TLS engine fails the handshake instead of seeing an abrupt close.
            await using var server = MisbehavingTcpServer.Start(MisbehaveMode.SendTlsHandshakeFailureAlert);
            var response = await Run($"https://127.0.0.1:{server.Port}/", useBouncyCastle);

            Assert.Equal(528, (int) response.StatusCode);
            AssertNetworkErrorCode(response, NetworkErrorCodes.TlsHandshakeFailure);
        }

        private static async Task<HttpResponseMessage> Run(string url, bool useBouncyCastle)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.UseBouncyCastle = useBouncyCastle;

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();

            var httpClient = HttpClientUtility.CreateHttpClient(endPoints, setting);
            var response = await httpClient.GetAsync(url);
            _ = await response.Content.ReadAsStringAsync();
            return response;
        }

        private static void AssertNetworkErrorCode(HttpResponseMessage response, string expectedToken)
        {
            Assert.True(
                response.Headers.TryGetValues("x-fluxzy-network-error", out var values),
                "Response is missing the x-fluxzy-network-error header.");

            Assert.Equal(expectedToken, values!.Single(), StringComparer.OrdinalIgnoreCase);
        }

        private static void AssertNetworkErrorCodeOneOf(HttpResponseMessage response, params string[] acceptedTokens)
        {
            Assert.True(
                response.Headers.TryGetValues("x-fluxzy-network-error", out var values),
                "Response is missing the x-fluxzy-network-error header.");

            var actual = values!.Single();
            Assert.Contains(actual, acceptedTokens, StringComparer.OrdinalIgnoreCase);
        }
    }
}
