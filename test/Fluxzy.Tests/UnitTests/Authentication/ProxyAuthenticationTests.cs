using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Authentication
{
    public class ProxyAuthenticationTests
    {
        [Theory]
        [InlineData("username", "password")]
        [InlineData("", "password")]
        [InlineData("username", "")]
        public async Task ProxyAuthenticateBasicOk(string username, string password)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.SetProxyAuthentication(ProxyAuthentication.Basic(username, password));

            await using var proxy = new Proxy(setting); 
            
            var endPoint = proxy.Run().First();

            using var httpClient = new HttpClient(new HttpClientHandler() {
                Proxy = new WebProxy(
                      new Uri($"http://{endPoint.Address}:{endPoint.Port}"),
                      false,
                      Array.Empty<string>(), 
                      new NetworkCredential(username, password)
                    )
            });

            var response = await httpClient.GetAsync(TestConstants.TestDomain);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Theory]
        [InlineData("username", "password")]
        [InlineData("", "password")]
        [InlineData("username", "")]
        public async Task ProxyAuthenticateBasicOkProvidedImpl(string username, string password)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            await using var proxy = new Proxy(setting,
                proxyAuthenticationMethod: new BasicAuthenticationMethod(username, password)); 
            
            var endPoint = proxy.Run().First();

            using var httpClient = new HttpClient(new HttpClientHandler() {
                Proxy = new WebProxy(
                      new Uri($"http://{endPoint.Address}:{endPoint.Port}"),
                      false,
                      Array.Empty<string>(), 
                      new NetworkCredential(username, password)
                    )
            });

            var response = await httpClient.GetAsync(TestConstants.TestDomain);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task ProxyAuthenticateBasicBadCredentials()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.SetProxyAuthentication(ProxyAuthentication.Basic("a", "b"));

            await using var proxy = new Proxy(setting); 
            
            var endPoint = proxy.Run().First();

            using var httpClient = new HttpClient(new HttpClientHandler() {
                Proxy = new WebProxy(
                      new Uri($"http://{endPoint.Address}:{endPoint.Port}"),
                      false,
                      Array.Empty<string>(), 
                      new NetworkCredential("d", "e")
                    )
            });

            await Assert.ThrowsAsync<HttpRequestException>(async () => {
                try
                {
                    await httpClient.GetAsync(TestConstants.TestDomain);
                }
                catch (HttpRequestException requestException) {
                    Assert.Equal(HttpRequestError.ProxyTunnelError, requestException.HttpRequestError);
                    throw; 
                }
            });
        }

        [Fact]
        public async Task ProxyAuthenticateBasicNoAuthProvided()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.SetProxyAuthentication(ProxyAuthentication.Basic("a", "b"));

            await using var proxy = new Proxy(setting); 
            
            var endPoint = proxy.Run().First();

            using var httpClient = new HttpClient(new HttpClientHandler() {
                Proxy = new WebProxy(
                      new Uri($"http://{endPoint.Address}:{endPoint.Port}"))
            });

            await Assert.ThrowsAsync<HttpRequestException>(async () => {
                try
                {
                    await httpClient.GetAsync(TestConstants.TestDomain);
                }
                catch (HttpRequestException requestException)
                {
                    Assert.Equal(HttpRequestError.ProxyTunnelError, requestException.HttpRequestError);
                    throw;
                }
            });
        }
    }
}
