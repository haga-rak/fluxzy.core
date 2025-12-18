// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.DotNetBridge;
using Fluxzy.Misc.Streams;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Handlers
{
    public class Http11SingleCallUrl
    {
        [Fact]
        public async Task Get_IIS()
        {
            using var handler = new FluxzyHttp11Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://extranet.2befficient.fr/Scripts/Core?v=RG4zfPZTCmDTC0sCJZC1Fx9GEJ_Edk7FLfh_lQ"
            );

            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

            Assert.True(response.IsSuccessStatusCode);
        }


        [Fact]
        public async Task Get_Control_Single_Headers()
        {
            using var handler = new FluxzyHttp11Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/get"
            );

            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            var contentText = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        [Fact]
        public async Task Get_With_200_Simple()
        {
            using var handler = new FluxzyHttp11Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/ip"
            );

            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            var contentText = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        [Fact]
        public async Task Get_With_204_No_Body()
        {
            using var handler = new FluxzyHttp11Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/status/204"
            );

            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            var contentText = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(contentText == string.Empty);
        }

        [Fact]
        public async Task Get_File_Small_UnknownSize()
        {
            using var handler = new FluxzyHttp11Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "https://sandbox.fluxzy.io/content-produce-unpredictable/130000/130000"
            );

            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            var contentLength = await (await response.Content.ReadAsStreamAsync()).DrainAsync();

            var actualLength =
                int.Parse(response.Headers.GetValues("actual-content-length").First());

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(actualLength, contentLength);
        }

        [Fact]
        public async Task Get_File_Large_UnknownSize()
        {
            using var handler = new FluxzyHttp11Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"{TestConstants.Http2Host}/content-produce-unpredictable/2300000/2300000"
            );

            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            var array = await response.Content.ReadAsByteArrayAsync();

            var actualLength =
                int.Parse(response.Headers.GetValues("actual-content-length").First());

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(actualLength, array.Length);
        }

        [Fact]
        public async Task Get_Control_Duplicate_Headers()
        {
            using var handler = new FluxzyHttp11Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/get"
            );

            requestMessage.Headers.Add("x-favorite-header", "1");
            requestMessage.Headers.Add("X-fAVorite-header", "2");

            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            var contentText = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        [Fact]
        public async Task Post_Data_Lt_Max_Frame()
        {
            using var handler = new FluxzyHttp11Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://{TestConstants.HttpBinHost}/post"
            );

            var bufferString = new string('a', 16 * 1024 - 9);

            var content = new StringContent(bufferString, Encoding.UTF8);
            content.Headers.ContentLength = content.Headers.ContentLength;

            requestMessage.Content = content;

            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            var contentText = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        [Fact]
        public async Task Post_Data_Gt_Max_Frame()
        {
            using var handler = new FluxzyHttp11Handler();
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

            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            var contentText = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        [Fact]
        public async Task Post_Data_Unknown_Size()
        {
            using var handler = new FluxzyHttp11Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://{TestConstants.HttpBinHost}/post"
            );

            using var randomStream = new RandomDataStream(9, 1024 * 124);
            var content = new StreamContent(randomStream, 8192);

            requestMessage.Content = content;

            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            var contentText = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);

            AssertHelpers
                .ControlHeaders(contentText, requestMessage)
                .ControlBody(randomStream.Hash);
        }

        [Fact]
        public async Task Get_With_InvalidHeaders()
        {
            using var handler = new FluxzyHttp11Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/get"
            );

            requestMessage.Headers.Add("Connection", "Keep-alive");

            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            var contentText = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Get_With_Extra_Column_Header()
        {
            using var handler = new FluxzyHttp11Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/get"
            );

            requestMessage.Headers.Add("x-Header-a", "ads");

            var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

            var contentText = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            AssertHelpers.ControlHeaders(contentText, requestMessage);
        }

        [Fact]
        public async Task Get_And_Cancel()
        {
            using var handler = new FluxzyHttp11Handler();
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(TimeoutConstants.Regular) };

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://{TestConstants.HttpBinHost}/get"
            );

            var source = new CancellationTokenSource();

            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                var responsePromise = httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead
                    , source.Token);
                source.Cancel();
                await responsePromise;
            });
        }
    }
}
