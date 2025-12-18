// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class PlainRequestOnSkippedSsl
    {
        [Fact]
        public async Task Validate()
        {
            var setting = FluxzySetting
                .CreateLocalRandomPort();

            setting.ConfigureRule()
                   .WhenAny()
                   .Do(new SkipSslTunnelingAction());

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting,
                handler => {
                    handler.ServerCertificateCustomValidationCallback =
                        (_, _, _, _) => true;
                });

            var url = "http://sandbox.fluxzy.io:8899/ip";

            for (int i = 0; i < 6; i++) {
                var response = await client.GetAsync(url);
                var fullResponseBody = await response.Content.ReadAsStringAsync();

                var statusCode = (int)response.StatusCode;

                Assert.Equal(200, statusCode);
            }
        }

    }
}
