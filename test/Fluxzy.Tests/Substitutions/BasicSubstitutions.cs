using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Tests.Substitutions.Actions;
using Xunit;

namespace Fluxzy.Tests.Substitutions
{
    public class BasicSubstitutions
    {
        [Theory]
        [InlineData(TestConstants.Http11Host)]
        [InlineData(TestConstants.Http2Host)]
        public async Task TestSimpleMock(string host)
        {
            var expectedContent = "Mocked !!!";

            var substitution = new ReturnsContentLengthSubstitution(expectedContent);

            await using var proxy = new AddHocConfigurableProxy(1, 10);

            proxy.StartupSetting.AddAlterationRulesForAny(new AddResponseBodyStreamSubstitutionAction(substitution));
            var endPoint = proxy.Run().First();

            using var clientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://{endPoint}")
            };

            using var httpClient = new HttpClient(clientHandler);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                $"{host}/global-health-check");

            using var response = await httpClient.SendAsync(requestMessage);
            var actualBodyString = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedContent, actualBodyString);
        }
    }
}
