// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Certificates;
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
        
        [Fact]
        public async Task MakeMultipleSelfCallCa()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();
            
            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();
            using var client = HttpClientUtility.CreateHttpClient(endPoints, setting);

            var certificate = setting.CaCertificate.GetX509Certificate();
            var certificateString = Encoding.UTF8.GetString(certificate.ExportToPem());

            for (int i = 0; i < 5; i++) {
                var response = await client.GetAsync($"http://127.0.0.1:{endPoints.First().Port}/ca");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                Assert.Equal(certificateString, content);
            }
        }

        [Fact]
        public async Task MakeMultipleSelfCallNothing()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();
            using var client = HttpClientUtility.CreateHttpClient(endPoints, setting);

            for (int i = 0; i < 4; i++)
            {
                var response = await client.GetAsync($"http://127.0.0.1:{endPoints.First().Port}/notmounted");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
            }
        }
    }
}
