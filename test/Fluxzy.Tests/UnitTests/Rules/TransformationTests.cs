// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Rules
{
    public class TransformationTests
    {
        [Theory]
        [InlineData(DecompressionMethods.None)]
        [InlineData(DecompressionMethods.GZip)]
        [InlineData(DecompressionMethods.Deflate)]
        [InlineData(DecompressionMethods.Brotli)]
        public async Task TestWithEncoding(DecompressionMethods method)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();
            var expectedResponse = "HTTP/1.0" + "Hello";

            setting.ConfigureRule().WhenAny()
                   .Do(new TransformTextResponseBodyAction(async (context, bodyReader) => {
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
        public async Task TestIgnoreBody(DecompressionMethods method)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.ConfigureRule().WhenAny()
                   .Transform((_, _) => Task.FromResult<BodyContent>("Hello"));

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
    }
}
