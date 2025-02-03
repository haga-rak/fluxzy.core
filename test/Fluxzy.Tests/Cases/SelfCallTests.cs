// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class SelfCallTests
    {
        [Fact]
        public async Task MakeMultipleSelfCall()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();
            
            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();
            using var client = HttpClientUtility.CreateHttpClient(endPoints, setting);

            for (int i = 0; i < 4; i++) {
                var response = await client.GetAsync($"http://127.0.0.1:{endPoints.First().Port}/welcome");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
            }
        }
    }
}
