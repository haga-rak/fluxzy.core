// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.DotNetBridge;
using Fluxzy.Tests.Common;
using Xunit;

namespace Fluxzy.Tests
{
    public class Http2ClientHandler
    {
        public static IEnumerable<object[]> GetHttpMethods {
            get
            {
                int[] checkLength = { 0, 152, 12464, 150002 };

                foreach (var length in checkLength) {
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
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

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
        [InlineData("https://fr.wiktionary.org/w/skins/Vector/resources/common/images/arrow-down.svg?9426f")]
        [InlineData("https://discord.com/assets/afe2828ad8a44f9ed87d.js")]
        [InlineData("https://wcpstatic.microsoft.com/mscc/lib/v2/wcp-consent.js")]
        [InlineData("https://services.gfe.nvidia.com/GFE/v1.0/dao/x64")]
        [InlineData("https://feedback.adrecover.com/ARWebService/checkCID")]
        [InlineData("https://registry.2befficient.io:40300/ip")]
        [InlineData("https://cds.taboola.com/?uid=7a5716a9-185b-4b54-8155-87f4b705c55f-tuct7ead376&src=tfa")]
        [InlineData("https://extranet.2befficient.fr/Scripts/Core?v=RG4zfPZTCmDTC0sCJZC1Fx9GEJ_Edk7FLfh_lQ")]
        public async Task Get_Error_Case(string url)
        {
            // Environment.SetEnvironmentVariable("EnableH2Tracing", "true");

            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(22) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                url
            );

            var response = await httpClient.SendAsync(requestMessage);
            await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Get_Control_Single_Headers()
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://registry.2befficient.io:40300/get"
            );

            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        [Fact]
        public async Task Get_With_204_No_Body()
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"{TestConstants.Http2Host}/content-produce/0/0"
            );

            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(contentText == string.Empty);
        }

        [Fact]
        public async Task Get_Control_Duplicate_Headers()
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://registry.2befficient.io:40300/get"
            );

            requestMessage.Headers.Add("x-favorite-header", "1");
            requestMessage.Headers.Add("X-fAVorite-header", "2");

            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        [Fact]
        public async Task Post_Data_Lt_Max_Frame()
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                "https://registry.2befficient.io:40300/post"
            );

            var bufferString = new string('a', 16 * 1024 - 9);

            var content = new StringContent(bufferString, Encoding.UTF8);
            content.Headers.ContentLength = content.Headers.ContentLength;

            requestMessage.Content = content;

            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        [Fact]
        public async Task Post_Data_Gt_Max_Frame()
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                "https://registry.2befficient.io:40300/post"
            );

            var bufferString = new string('a', 16 * 1024 + 10);

            var content = new StringContent(bufferString, Encoding.UTF8);
            content.Headers.ContentLength = content.Headers.ContentLength;

            requestMessage.Content = content;

            requestMessage.ToHttp11String();

            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        [Fact]
        public async Task Post_Data_Unknown_Size()
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                "https://registry.2befficient.io:40300/post"
            );

            using var randomStream = new RandomDataStream(9, 1024 * 124);
            var content = new StreamContent(randomStream, 8192);

            requestMessage.Content = content;

            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.True(response.IsSuccessStatusCode);

            AssertHelpers
                .ControlHeaders(contentText, requestMessage)
                .ControlBody(randomStream.Hash);
        }

        [Fact]
        public async Task Get_With_InvalidHeaders()
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://registry.2befficient.io:40300/get"
            );

            requestMessage.Headers.Add("Connection", "Keep-alive");

            var response = await httpClient.SendAsync(requestMessage);
            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Get_With_Extra_Column_Header()
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://registry.2befficient.io:40300/get"
            );

            requestMessage.Headers.Add("x-Header-a", "ads");

            var response = await httpClient.SendAsync(requestMessage);

            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        [Fact]
        public async Task Get_And_Cancel()
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://registry.2befficient.io:40300/get"
            );

            var source = new CancellationTokenSource();

            await Assert.ThrowsAsync<TaskCanceledException>(async () => {
                var responsePromise = httpClient.SendAsync(requestMessage, source.Token);
                source.Cancel();
                await responsePromise;
            });
        }

        [Fact]
        public async Task Get_304()
        {
            using var handler = new FluxzyHttp2Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://registry.2befficient.io:40300/status/304"
            );

            requestMessage.Headers.Add("x-Header-a", "ads");

            var response = await httpClient.SendAsync(requestMessage);

            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Assert.Equal((HttpStatusCode) 304, response.StatusCode);
            Assert.Equal(string.Empty, contentText);
        }
    }
}
