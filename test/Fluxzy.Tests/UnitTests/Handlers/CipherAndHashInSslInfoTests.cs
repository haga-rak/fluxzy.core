// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Threading.Tasks;
using Fluxzy.Clients.DotNetBridge;
using Fluxzy.Core;
using Fluxzy.Writers;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Handlers
{
    /// <summary>
    /// Confirms that <see cref="SslInfo"/> exposes the negotiated cipher suite,
    /// cipher algorithm and hash algorithm regardless of the SSL provider.
    /// The OsDefault path populates them from <c>SslStream</c>; the BouncyCastle
    /// constructor currently does not (see <c>SslInfo(FluxzyClientProtocol, bool)</c>).
    /// </summary>
    public class CipherAndHashInSslInfoTests
    {
        [Theory]
        [InlineData(SslProvider.OsDefault)]
        [InlineData(SslProvider.BouncyCastle)]
        public async Task NegotiatedCipherSuite_IsPopulated(SslProvider sslProvider)
        {
            var sslInfo = await GetSslInfoForGoogle(sslProvider);

            Assert.NotNull(sslInfo);
            Assert.NotEqual(default(TlsCipherSuite), sslInfo.NegotiatedCipherSuite);
        }

        [Theory]
        [InlineData(SslProvider.OsDefault)]
        [InlineData(SslProvider.BouncyCastle)]
        public async Task CipherAlgorithm_IsPopulated(SslProvider sslProvider)
        {
            var sslInfo = await GetSslInfoForGoogle(sslProvider);

            Assert.NotNull(sslInfo);
            Assert.NotEqual(CipherAlgorithmType.None, sslInfo.CipherAlgorithm);
        }

        // OsDefault is intentionally excluded: SslStream.HashAlgorithm is obsolete and returns
        // None for TLS 1.3 on OpenSSL (Linux/macOS), even though the negotiated suite carries
        // a hash. The BouncyCastle path derives Sha256/Sha384 from the suite name and so
        // exposes a useful value where .NET no longer does.
        [Theory]
        [InlineData(SslProvider.BouncyCastle)]
        public async Task HashAlgorithm_IsPopulated(SslProvider sslProvider)
        {
            var sslInfo = await GetSslInfoForGoogle(sslProvider);

            Assert.NotNull(sslInfo);
            Assert.NotEqual(HashAlgorithmType.None, sslInfo.HashAlgorithm);
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
