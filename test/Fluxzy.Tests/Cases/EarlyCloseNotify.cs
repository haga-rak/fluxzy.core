using Fluxzy.Tests._Fixtures;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class EarlyCloseNotify
    {
        [Theory]
        [InlineData("BouncyCastle")]
        [InlineData("OSDefault")]
        public async Task Run_Until_Close_Notify(string sslEngine)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                // ENABLE only for Windows now 
                return;
            }

            var count = 10;
            var url = "https://espace-client.mma.fr/connexion/main-es2015.b75177084686b8f6f756.js"; 

            await using var proxy = new AddHocConfigurableProxy(1, 10);

            proxy.StartupSetting.UseBouncyCastle = sslEngine == "BouncyCastle";

            var endPoint = proxy.Run().First();

            using var clientHandler = new HttpClientHandler();

            clientHandler.Proxy = new WebProxy($"http://{endPoint}");

            using var httpClient = new HttpClient(clientHandler);

            for (int i = 0; i < count; i++) {
                var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                                       url);

                using var response = await httpClient.SendAsync(requestMessage);
                _ = await response.Content.ReadAsStringAsync();

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }
    }
}
