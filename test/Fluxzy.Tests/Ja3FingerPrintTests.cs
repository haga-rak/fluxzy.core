// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Security.Authentication;
using Fluxzy.Tests._Fixtures;
using System.Text.Json;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Xunit;

namespace Fluxzy.Tests
{
    public class Ja3FingerPrintTests
    {
        [Fact]  
        public async Task TestJa3FingerPrint()
        {
            var url = "https://tools.scrapfly.io/api/tls";

            await using var proxy = new AddHocConfigurableProxy(1, 10, setting => {
                setting.UseBouncyCastleSslEngine();
                setting.AddAlterationRulesForAny(new SetCiphersAction(
                        "TLS_AES_128_GCM_SHA256",
                        "TLS_AES_256_GCM_SHA384",
                        "TLS_CHACHA20_POLY1305_SHA256",
                        "TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256",
                        "TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256",
                        "TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384",
                        "TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384",
                        "TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256",
                        "TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256",
                        "TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA",
                        "TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA",
                        "TLS_RSA_WITH_AES_128_GCM_SHA256",
                        "TLS_RSA_WITH_AES_256_GCM_SHA384",
                        "TLS_RSA_WITH_AES_128_CBC_SHA",
                        "TLS_RSA_WITH_AES_256_CBC_SHA"
                    )
                );
            });

            using var httpClient = proxy.RunAndGetClient();
            using var response = await httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var ja3Response = JsonSerializer.Deserialize<Ja3FingerPrintResponse>(responseString);

            var chrome131Response = Ja3FingerPrintRepository.FingerPrints["CHROME_131"];

            Assert.NotNull(ja3Response);
            Assert.Equal(chrome131Response.Ja3n, ja3Response.Ja3n);
            Assert.Equal(chrome131Response.Ja3nDigest, ja3Response.Ja3nDigest);
        }
    }
}