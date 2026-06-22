// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json;
using System.Threading.Tasks;
using Fluxzy.Clients.Ssl;
using Fluxzy.Rules.Actions;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests
{
    public class ImpersonateTests
    {
        [Theory]
        [InlineData("_Files/Others/template-Edge_Windows_131.json")]
        public async Task CheckSignatureFromFile(string nameOrConfigfile)
        {
            var testUrl = "https://check.ja3.zone/";

            await using var proxy = new AddHocConfigurableProxy(1, 10,
                configureSetting: setting => {
                    setting.UseBouncyCastleSslEngine();
                    setting.AddAlterationRulesForAny(new ImpersonateAction(nameOrConfigfile));
                });


            var impersonateLoader = ImpersonateConfigurationManager.Instance.LoadConfiguration(nameOrConfigfile)!;

            var rawFingerPrint = TlsFingerPrint.ParseFromJa3(impersonateLoader.NetworkSettings.Ja3FingerPrint);
            var expectedJa3 = rawFingerPrint.ToString(true);

            using var httpClient = proxy.RunAndGetClient();
            using var response = await httpClient.GetAsync(testUrl);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            var ja3Response = JsonSerializer.Deserialize<Ja3FingerPrintResponse>(responseString);

            Assert.NotNull(ja3Response);
            Assert.Equal(expectedJa3, ja3Response.NormalizedFingerPrint);
        }

        [Theory]
        [InlineData("Chrome_Windows_131")]
        [InlineData("Firefox_Windows_133")]
        [InlineData("Edge_Windows_131")]
        [InlineData("Firefox_Windows_142")]
        public async Task CheckSignature(string nameOrConfigfile)
        {
            var testUrl = "https://check.ja3.zone/";

            await using var proxy = new AddHocConfigurableProxy(1, 10,
                configureSetting: setting => {
                    setting.UseBouncyCastleSslEngine();
                    setting.AddAlterationRulesForAny(new ImpersonateAction(nameOrConfigfile));
                });

            var impersonateLoader = ImpersonateConfigurationManager.Instance.LoadConfiguration(nameOrConfigfile)!;

            var rawFingerPrint = TlsFingerPrint.ParseFromJa3(impersonateLoader.NetworkSettings.Ja3FingerPrint);
            var expectedJa3 = rawFingerPrint.ToString(true);

            using var httpClient = proxy.RunAndGetClient();
            using var response = await httpClient.GetAsync(testUrl);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            var ja3Response = JsonSerializer.Deserialize<Ja3FingerPrintResponse>(responseString);

            Assert.NotNull(ja3Response);
            Assert.Equal(expectedJa3, ja3Response.NormalizedFingerPrint);
        }
    }
}
