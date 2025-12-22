// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Fluxzy.Rules.Actions;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Rules
{
    public class ResponseTransformationTests
    {
        [Fact]
        public async Task ResponseBodyStartSample()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();
            var expectedResponse = "HTTP/1.1" + "Hello";

            setting.ConfigureRule().WhenAny()
                   .TransformResponse((_, originalContent) => Task.FromResult(originalContent + "Hello"));

            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();

            var url = $"https://sandbox.fluxzy.io/protocol"; // return "HTTP/1.0"

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
            var expectedResponse = "HTTP/1.1";

            setting.ConfigureRule().WhenAny()
                   .TransformResponse(async (_, originalContent) =>  (BodyContent?) null); // Return null to keep the original content without change

            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();

            var url = $"https://sandbox.fluxzy.io/protocol"; // return "HTTP/1.0"

            using var client = HttpClientUtility.CreateHttpClient(endPoints, setting);

            var response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(expectedResponse, content);
        }

        [Fact]
        public async Task ResponseBodyNoConsume()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.ConfigureRule().WhenAny()
                   .TransformResponse(async (_, _) =>  (BodyContent?) "hello");

            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();

            var url = $"https://sandbox.fluxzy.io/protocol"; // return "HTTP/1.0"

            using var client = HttpClientUtility.CreateHttpClient(endPoints, setting);

            var response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal("hello", content);
        }
        
        [Fact]
        public async Task ResponseBodySampleString()
        {
            var expectedResponse = "http/1.1";

            var fluxzySetting = FluxzySetting.CreateLocalRandomPort();
            fluxzySetting.ConfigureRule().WhenAny()
                   .TransformResponse(
                       (_, originalContent) => Task.FromResult(originalContent.ToLowerInvariant()));

            await using var proxy = new Proxy(fluxzySetting);

            var endPoints = proxy.Run();

            var url = $"https://sandbox.fluxzy.io/protocol"; // return "HTTP/1.0"

            using var client = HttpClientUtility.CreateHttpClient(endPoints, fluxzySetting);

            var response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(expectedResponse, content);
        }

        [Fact]
        public async Task ResponseBodySampleStream()
        {
            var expectedResponse = "A";

            var fluxzySetting = FluxzySetting.CreateLocalRandomPort();
            fluxzySetting.ConfigureRule().WhenAny()
                   .TransformResponse(
                       (_, originalStream) => Task.FromResult<Stream?>(new MemoryStream(new byte[] { 65 })));

            await using var proxy = new Proxy(fluxzySetting);

            var endPoints = proxy.Run();

            var url = $"https://sandbox.fluxzy.io/protocol"; // return "HTTP/1.0"

            using var client = HttpClientUtility.CreateHttpClient(endPoints, fluxzySetting);

            var response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(expectedResponse, content);
        }

        [Fact]
        public async Task ResponseBodySampleStreamRead()
        {
            var fluxzySetting = FluxzySetting.CreateLocalRandomPort();
            fluxzySetting.ConfigureRule().WhenAny()
                   .TransformResponse(
                       async (_, originalStream) => {
                           var length = await originalStream.DrainAsync();
                           return new MemoryStream(Encoding.UTF8.GetBytes(length.ToString()));
                       });

            await using var proxy = new Proxy(fluxzySetting);

            var endPoints = proxy.Run();

            var url = $"https://sandbox.fluxzy.io/protocol"; // return "HTTP/1.0"

            using var client = HttpClientUtility.CreateHttpClient(endPoints, fluxzySetting);

            var response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal("8", content);
        }

        [Theory]
        [InlineData(DecompressionMethods.None)]
        [InlineData(DecompressionMethods.GZip)]
        [InlineData(DecompressionMethods.Deflate)]
        [InlineData(DecompressionMethods.Brotli)]
        public async Task ResponseBodyWithEncoding(DecompressionMethods method)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();
            var expectedResponse = "HTTP/1.1" + "Hello";

            setting.ConfigureRule().WhenAny()
                   .Do(new TransformResponseBodyAction(async (_, bodyReader) => {
                       var body = await bodyReader.ConsumeAsString();
                       return body + "Hello";
                   }));

            await using var proxy = new Proxy(setting);

            var endPoints = proxy.Run();

            var url = $"https://sandbox.fluxzy.io/protocol";

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

            var url = $"https://sandbox.fluxzy.io/protocol";

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

            var url = $"https://sandbox.fluxzy.io/protocol"; // return "HTTP/1.0"

            using var client = HttpClientUtility.CreateHttpClient(endPoints, setting);

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
            Assert.Contains("A rule execution failure has occured.", content);
        }
    }
}
