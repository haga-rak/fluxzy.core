// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.DotNetBridge;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Handlers
{
    public class Http2ClientHandler
    {
        public static IEnumerable<object[]> GetHttpMethods
        {
            get
            {
                int[] checkLength = { 0, 152, 12464, 150002 };

                foreach (var length in checkLength)
                {
                    yield return new object[] { HttpMethod.Get, length };
                    yield return new object[] { HttpMethod.Post, length };
                    yield return new object[] { HttpMethod.Put, length };
                    yield return new object[] { HttpMethod.Patch, length };
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetHttpMethods))]
        public async Task Check_Global_Health(HttpMethod method, int length)
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(method,
                $"{TestConstants.Http2Host}/global-health-check?dsf=sdfs&dsf=3");

            await using var randomStream = new RandomDataStream(48, length, true);
            await using var hashedStream = new HashedStream(randomStream);

            requestMessage.Content = new StreamContent(hashedStream);
            requestMessage.Headers.Add("X-Identifier", "Simple header");

            using var response = await httpClient.SendAsync(requestMessage);
            await AssertionHelper.ValidateCheck(requestMessage, hashedStream.Hash, response);
            Assert.True(response.IsSuccessStatusCode);
        }

        [Theory]
        [InlineData("https://fr.wiktionary.org/static/images/icons/wiktionary.svg")]
        [InlineData("https://wcpstatic.microsoft.com/mscc/lib/v2/wcp-consent.js")]
        [InlineData("https://services.gfe.nvidia.com/GFE/v1.0/dao/x64")]
        [InlineData($"https://{TestConstants.HttpBinHost}/ip")]
        [InlineData("https://cds.taboola.com/?uid=7a5716a9-185b-4b54-8155-87f4b705c55f-tuct7ead376&src=tfa")]
        public async Task Get_Error_Case(string url)
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Extended) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                url
            );

            var response = await httpClient.SendAsync(requestMessage);

            await response.Content.ReadAsStringAsync();

            Assert.InRange((int)response.StatusCode, 200, 299);
        }

        [Fact]
        public async Task Get_Control_Single_Headers()
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/get"
            );

            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        [Fact]
        public async Task Get_With_204_No_Body()
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"{TestConstants.Http2Host}/content-produce/0/0"
            );

            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(contentText == string.Empty);
        }

        [Fact]
        public async Task Get_Control_Duplicate_Headers()
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/get"
            );

            requestMessage.Headers.Add("x-favorite-header", "1");
            requestMessage.Headers.Add("X-fAVorite-header", "2");

            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        [Fact]
        public async Task Post_Data_Lt_Max_Frame()
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://{TestConstants.HttpBinHost}/post"
            );

            var bufferString = new string('a', 16 * 1024 - 9);

            var content = new StringContent(bufferString, Encoding.UTF8);
            content.Headers.ContentLength = content.Headers.ContentLength;

            requestMessage.Content = content;

            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        [Fact]
        public async Task Post_Data_Gt_Max_Frame()
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://{TestConstants.HttpBinHost}/post"
            );

            var bufferString = new string('a', 16 * 1024 + 10);

            var content = new StringContent(bufferString, Encoding.UTF8);
            content.Headers.ContentLength = content.Headers.ContentLength;

            requestMessage.Content = content;

            requestMessage.ToHttp11String();

            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        /// <summary>
        /// NOTE out of diskpace in runner mail fail this test
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Post_Data_Unknown_Size()
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://{TestConstants.HttpBinHost}/post"
            );

            using var randomStream = new RandomDataStream(9, 1024 * 124);
            var content = new StreamContent(randomStream, 8192);

            requestMessage.Content = content;

            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);

            AssertHelpers
                .ControlHeaders(contentText, requestMessage)
                .ControlBody(randomStream.Hash);
        }

        [Fact]
        public async Task Get_With_InvalidHeaders()
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/get"
            );

            requestMessage.Headers.Add("Connection", "Keep-alive");

            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Get_With_Extra_Column_Header()
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/get"
            );

            requestMessage.Headers.Add("x-Header-a", "ads");

            var response = await httpClient.SendAsync(requestMessage);

            var contentText = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        [Fact]
        public async Task Get_And_Cancel()
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/get"
            );

            var source = new CancellationTokenSource();

            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                var responsePromise = httpClient.SendAsync(requestMessage, source.Token);
                source.Cancel();
                await responsePromise;
            });
        }

        [Fact]
        public async Task Get_304()
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/status/304"
            );

            requestMessage.Headers.Add("x-Header-a", "ads");

            var response = await httpClient.SendAsync(requestMessage);

            var contentText = await response.Content.ReadAsStringAsync();

            Assert.Equal((HttpStatusCode)304, response.StatusCode);
            Assert.Equal(string.Empty, contentText);
        }
    }
}
