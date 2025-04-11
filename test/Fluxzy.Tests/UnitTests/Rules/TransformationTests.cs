// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Rules
{
    public class TransformationTests
    {
        [Fact]
        public async Task ResponseBodyStartSample()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();
            var expectedResponse = "HTTP/1.0" + "Hello";

            setting.ConfigureRule().WhenAny()
                   .TransformResponse((_, originalContent) => Task.FromResult(originalContent + "Hello"));

            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();

            var url = $"https://sandbox.smartizy.com/protocol"; // return "HTTP/1.0"

            using var client = HttpClientUtility.CreateHttpClient(endPoints, setting);

            var response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(expectedResponse, content);
        }

        [Fact]
        public async Task ResponseBodyNoChange()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();
            var expectedResponse = "HTTP/1.0";

            setting.ConfigureRule().WhenAny()
                   .TransformResponse(async (_, originalContent) =>  (BodyContent?) null); // Return null to keep the original content without change

            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();

            var url = $"https://sandbox.smartizy.com/protocol"; // return "HTTP/1.0"

            using var client = HttpClientUtility.CreateHttpClient(endPoints, setting);

            var response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(expectedResponse, content);
        }


        [Theory]
        [InlineData(DecompressionMethods.None)]
        [InlineData(DecompressionMethods.GZip)]
        [InlineData(DecompressionMethods.Deflate)]
        [InlineData(DecompressionMethods.Brotli)]
        public async Task ResponseBodyWithEncoding(DecompressionMethods method)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();
            var expectedResponse = "HTTP/1.0" + "Hello";

            setting.ConfigureRule().WhenAny()
                   .Do(new TransformTextResponseBodyAction(async (_, bodyReader) => {
                       var body = await bodyReader.ConsumeAsString();
                       return body + "Hello";
                   }));

            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();

            var url = $"https://sandbox.smartizy.com/protocol";

            using var client = HttpClientUtility.CreateHttpClient(endPoints, setting, 
                configureHandler: httpClientHandler => {
                    httpClientHandler.AutomaticDecompression = method;
                });

            var response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(expectedResponse, content);
        }

        [Theory]
        [InlineData(DecompressionMethods.None)]
        [InlineData(DecompressionMethods.GZip)]
        [InlineData(DecompressionMethods.Deflate)]
        [InlineData(DecompressionMethods.Brotli)]
        public async Task ResponseBodyIgnoreOriginalBody(DecompressionMethods method)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.ConfigureRule().WhenAny()
                   .TransformResponse((_, _) => Task.FromResult<BodyContent?>("Hello"));

            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();

            var url = $"https://sandbox.smartizy.com/protocol";

            using var client = HttpClientUtility.CreateHttpClient(endPoints, setting, 
                configureHandler: httpClientHandler => {
                    httpClientHandler.AutomaticDecompression = method;
                });

            var response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal("Hello", content);
        }

        [Fact]
        public async Task ResponseBodyConsumeViolation()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.ConfigureRule().WhenAny()
                   .TransformResponse(async (_, originalContent) => {
                       await originalContent.ConsumeAsBytes();
                       return (BodyContent?)null;
                   }); // Return null to keep the original content without change

            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();

            var url = $"https://sandbox.smartizy.com/protocol"; // return "HTTP/1.0"

            using var client = HttpClientUtility.CreateHttpClient(endPoints, setting);

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
            Assert.Contains("A rule execution failure has occured.", content);
        }
    }
}
