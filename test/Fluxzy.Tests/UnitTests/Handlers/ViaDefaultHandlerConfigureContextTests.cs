// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Http;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Fluxzy.Clients.DotNetBridge;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.Ssl;
using Fluxzy.Core;
using Fluxzy.Rules.Actions;
using Fluxzy.Writers;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Handlers
{
    public class ViaDefaultHandlerConfigureContextTests
    {
        /// <summary>
        /// Configure a custom TLS FingerPrint with the FluxzyDefaultHandler which is a HttpMessageHandler.
        /// SSL Engine must be set to BouncyCastle for TLS fingerprint customization.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [Fact]
        public async Task Impersonate()
        {
            // Arrange
            await using var tcpProvider = ITcpConnectionProvider.Default;

            using var handler = new FluxzyDefaultHandler(SslProvider.BouncyCastle, tcpProvider, new EventOnlyArchiveWriter());

            var configuration = ImpersonateConfigurationManager
                                    .Instance.LoadConfiguration(
                                        ImpersonateProfileManager.Chrome131Windows)!;
            
            var tlsFingerPrint = TlsFingerPrint.ParseFromImpersonateConfiguration(configuration);

            handler.ConfigureContext = (exchangeContext) => {
                exchangeContext.AdvancedTlsSettings.TlsFingerPrint = tlsFingerPrint;
                exchangeContext.AdvancedTlsSettings.H2StreamSetting = new H2StreamSetting() 
                {
                    // Configure H2 settings
                };
            };

            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://check.ja3.zone/"
            );

            // Act
            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            var ja3Response = JsonSerializer.Deserialize<Ja3FingerPrintResponse>(responseString);

            var expectedJa3 = 
                TlsFingerPrint.ParseFromJa3(configuration.NetworkSettings.Ja3FingerPrint)
                              .ToString(true);

            // Assert
            Assert.NotNull(ja3Response);
            Assert.Equal(expectedJa3, ja3Response.NormalizedFingerPrint);
        }
    }
}
