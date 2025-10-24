// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class EarlyHintTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Validate(bool http2)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.UseBouncyCastleSslEngine();

            setting.ConfigureRule()
                   .WhenAny()
                   .Do(new ImpersonateAction(ImpersonateProfileManager.Chrome131Windows));

            if (!http2) {
                setting.ConfigureRule()
                       .WhenAny()
                       .Do(new ForceHttp11Action());
            }

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);
            
            var response = await client.GetAsync("https://testnet.monad.xyz");

            await (await response.Content.ReadAsStreamAsync()).CopyToAsync(Stream.Null); 

            var response2 = await client.GetAsync("https://testnet.monad.xyz/android-chrome-192x192.png");

            Assert.Equal(200, (int) response.StatusCode);
            Assert.Equal(200, (int) response2.StatusCode);
        }
    }
}
