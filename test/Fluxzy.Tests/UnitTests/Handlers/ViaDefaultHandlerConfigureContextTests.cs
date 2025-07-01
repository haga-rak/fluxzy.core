// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Http;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Fluxzy.Clients.DotNetBridge;
using Fluxzy.Clients.Ssl;
using Fluxzy.Core;
using Fluxzy.Rules.Actions;
using Fluxzy.Writers;
using Org.BouncyCastle.Tls;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Handlers
{
    public class ViaDefaultHandlerConfigureContextTests
    {
        [Fact]
        public async Task Impersonate()
        {
            await using var tcpProvider = ITcpConnectionProvider.Default;

            using var handler = new FluxzyDefaultHandler(SslProvider.BouncyCastle, tcpProvider, new EventOnlyArchiveWriter());

            var configuration = ImpersonateConfigurationManager.Instance
                                                               .LoadConfiguration(ImpersonateProfileManager
                                                                   .Chrome131Windows)
                                ??
                                throw new InvalidOperationException("Configuration not found");

            var fingerPrint = TlsFingerPrint.ParseFromJa3(
                configuration.NetworkSettings.Ja3FingerPrint,
                configuration.NetworkSettings.GreaseMode,
                signatureAndHashAlgorithms:
                configuration.NetworkSettings
                             .SignatureAlgorithms?.Select(s =>
                                 SignatureAndHashAlgorithm.GetInstance(SignatureScheme.GetHashAlgorithm(s),
                                     SignatureScheme.GetSignatureAlgorithm(s))
                             ).ToList(),
                earlyShardGroups: configuration.NetworkSettings.EarlySharedGroups);

            handler.ConfigureContext = (exchangeContext) => {
                exchangeContext.AdvancedTlsSettings.TlsFingerPrint = fingerPrint;
            };

            var rawFingerPrint = TlsFingerPrint.ParseFromJa3(configuration.NetworkSettings.Ja3FingerPrint);
            var expectedJa3 = rawFingerPrint.ToString(true);

            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://check.ja3.zone/"
            );

            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            var ja3Response = JsonSerializer.Deserialize<Ja3FingerPrintResponse>(responseString);

            Assert.NotNull(ja3Response);
            Assert.Equal(expectedJa3, ja3Response.NormalizedFingerPrint);
        }
    }
}
