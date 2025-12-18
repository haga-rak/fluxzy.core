// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Fluxzy.Tests.Sandbox.Models;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Rules
{
    public class RequestTransformationTests
    {
        [Fact]
        public async Task RequestBodyString()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.ConfigureRule().WhenAny()
                   .TransformRequest((_, originalContent) => Task.FromResult("Hello"));

            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();

            var url = $"https://sandbox.fluxzy.io/global-health-check"; // return "HTTP/1.0"

            using var client = HttpClientUtility.CreateHttpClient(endPoints, setting);

            var postContent = new StringContent(new string('a', 10), Encoding.UTF8);

            var response = await client.PostAsync(url, postContent);
            response.EnsureSuccessStatusCode();

            var rawResponseString = await response.Content.ReadAsStringAsync();

            var checkResult = JsonSerializer.Deserialize<HealthCheckResult>(rawResponseString,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.Equal(5, checkResult!.RequestContent.Length);
        }

        [Fact]
        public async Task RequestBodyStringConcat()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.ConfigureRule().WhenAny()
                   .TransformRequest((_, originalContent) => Task.FromResult(originalContent + "Hello"));

            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();

            var url = $"https://sandbox.fluxzy.io/global-health-check"; // return "HTTP/1.0"

            using var client = HttpClientUtility.CreateHttpClient(endPoints, setting);

            var postContent = new StringContent(new string('a', 10), Encoding.UTF8);

            var response = await client.PostAsync(url, postContent);
            response.EnsureSuccessStatusCode();

            var checkResult = JsonSerializer.Deserialize<HealthCheckResult>(await response.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.Equal(15, checkResult!.RequestContent.Length);
        }

        [Fact]
        public async Task RequestBodyNoConsume()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.ConfigureRule().WhenAny()
                   .TransformRequest((_, originalContent) => Task.FromResult(new BodyContent("Hello"))!);

            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();

            var url = $"https://sandbox.fluxzy.io/global-health-check"; // return "HTTP/1.0"

            using var client = HttpClientUtility.CreateHttpClient(endPoints, setting);

            var postContent = new StringContent(new string('a', 10), Encoding.UTF8);

            var response = await client.PostAsync(url, postContent);
            response.EnsureSuccessStatusCode();

            var checkResult = JsonSerializer.Deserialize<HealthCheckResult>(await response.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.Equal(5, checkResult!.RequestContent.Length);
        }
    }
}
