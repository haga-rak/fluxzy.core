// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Tests._Fixtures;
using System.Text.Json;
using System.Threading.Tasks;
using Fluxzy.Clients.Ssl;
using Fluxzy.Rules.Actions;
using Xunit;

namespace Fluxzy.Tests
{
    public class Ja3FingerPrintTests
    {
        [Theory]
        [InlineData("769,49195-49199-49196-49200-52393-52392-52244-52243-49161-49171-49162-49172-156-157-47-53-10,65281-0-23-35-13-5-18-16-11-10-21,29-23-24,0")]
        [InlineData("772,4865-4866-4867-49195-49199-49196-49200-52393-52392-49171-49172-156-157-47-53,65281-65037-51-45-35-17513-10-11-43-27-5-13-23-18-0-16,4588-29-23-24,0")]
        [InlineData("772,,,4588-29-23-24,")]
        public void Format_Parse_Unparse(string originalFingerPrint)
        {
            var fingerPrint = Ja3FingerPrint.Parse(originalFingerPrint);
            var value = fingerPrint.ToString();

            Assert.Equal(originalFingerPrint, value);
        }

        [Theory]
        [InlineData("772,4865-4866-4867-49195-49199-49196-49200-52393-52392-49171-49172-156-157-47-53,0-5-10-11-13-16-18-23-27-35-43-45-51-17513-65281,4588-29-23-24,0")]
        [InlineData("772,4865-4866-4867-49195-49199-49196-49200-52393-52392-49171-49172-156-157-47-53,0-5-10-11-13-16-18-23-27-43-45-51-17513-65281,4588-29-23-24,0")]
        public async Task TestJa3FingerPrint_NoEch(string ja3)
        {
            var testUrl = "https://tools.scrapfly.io/api/tls";

            var originalFingerPrint = ja3; 

            await using var proxy = new AddHocConfigurableProxy(1, 10, configureSetting : setting => {
                setting.UseBouncyCastleSslEngine();
                setting.AddAlterationRulesForAny(new SetJa3FingerPrintAction(originalFingerPrint));
            });

            using var httpClient = proxy.RunAndGetClient();
            using var response = await httpClient.GetAsync(testUrl);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            var ja3Response = JsonSerializer.Deserialize<Ja3FingerPrintResponse>(responseString);
            
            Assert.NotNull(ja3Response);
            Assert.Equal(originalFingerPrint, ja3Response.Ja3n);
            //Assert.Equal(chrome131Response.Ja3nDigest, ja3Response.Ja3nDigest);
        }
    }
}