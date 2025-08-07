// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class SubsequentSslClose
    {
        [Theory, CombinatorialData]
        public async Task Validate(
            [CombinatorialValues("hello world!", "")] string responseString,
            [CombinatorialValues(true, false)] bool closeTransportFirst,
            [CombinatorialValues(true, false)] bool useBouncyCastle
            )
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            if (useBouncyCastle)
                setting.UseBouncyCastleSslEngine();

            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            var server = new ConnectionCloseTestServer(index => index >= 2, responseString, closeTransportFirst);
            await using var serverInstance = server.Start();

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);

            var url = $"https://local.fluxzy.io:{serverInstance.Port}/";

            for (int i = 0; i < 10; i++) {
                using var response = await client.GetAsync(url);
                var fullResponseBody = await response.Content.ReadAsStringAsync();

                var statusCode = (int) response.StatusCode;

                Assert.Equal(200, statusCode);
                Assert.Equal(responseString, fullResponseBody);
            }
        }
    }

}
