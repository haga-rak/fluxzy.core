// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Fluxzy.Rules.Actions;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class RandomConnectionBehaviourTest
    {
        public RandomConnectionBehaviourTest()
        {
            Environment.SetEnvironmentVariable("SSLKEYLOGFILE", "D:\\poubelle\\keylog.txt");
        }

        [Theory, CombinatorialData]
        public async Task ValidateClose(
            [CombinatorialValues("hello world!", "")] string responseString,
            [CombinatorialValues(true, false)] bool closeTransportFirst,
            [CombinatorialValues(true, false)] bool useBouncyCastle
            )
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            if (useBouncyCastle)
                setting.UseBouncyCastleSslEngine();

            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            var server = new ConnectionCloseTestServer(index => index >= 2, responseString, closeTransportFirst, false);
            await using var serverInstance = server.Start();

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);

            var url = $"https://local.fluxzy.io:{serverInstance.Port}/";

            for (int i = 0; i < 10; i++) {
                using var response = await client.GetAsync(url);
                var fullResponseBody = await response.Content.ReadAsStringAsync();

                var statusCode = (int) response.StatusCode;

                Assert.Equal(200, statusCode);
                Assert.Equal(responseString, fullResponseBody);
            }
        }

        [Theory, CombinatorialData]
        public async Task ValidateCloseNoContentLength(
            [CombinatorialValues("hello world!", "")] string responseString,
            [CombinatorialValues(false)] bool closeTransportFirst,
            [CombinatorialValues(true, false)] bool useBouncyCastle
            )
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            if (useBouncyCastle)
                setting.UseBouncyCastleSslEngine();

            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            var server = new ConnectionCloseTestServer(index => true, 
                responseString, closeTransportFirst, false, ommitContentLength: true);
            await using var serverInstance = server.Start();

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);

            var url = $"https://local.fluxzy.io:{serverInstance.Port}/";

            for (int i = 0; i < 10; i++) {
                using var response = await client.GetAsync(url);
                var fullResponseBody = await response.Content.ReadAsStringAsync();

                var statusCode = (int) response.StatusCode;

                Assert.Equal(200, statusCode);
                Assert.Equal(responseString, fullResponseBody);
            }
        }

        [Theory, CombinatorialData]
        public async Task ValidateEmptyBody(
            [CombinatorialValues("hello world!", "")] string responseString,
            [CombinatorialValues(true, false)] bool closeTransportFirst,
            [CombinatorialValues(true, false)] bool useBouncyCastle
            )
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            if (useBouncyCastle)
                setting.UseBouncyCastleSslEngine();

            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            var server = new ConnectionCloseTestServer(index => index >= 10000, responseString,
                closeTransportFirst, true);

            await using var serverInstance = server.Start();

            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);

            var url = $"https://local.fluxzy.io:{serverInstance.Port}/";

            var tasks = new List<Task>();

            for (int i = 0; i < 20; i++) {
                var task = DoQuery();
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            async Task DoQuery()
            {
                using var response = await client.GetAsync(url);
                var fullResponseBody = await response.Content.ReadAsStringAsync();

                var statusCode = (int) response.StatusCode;

                Assert.Equal(204, statusCode);
                Assert.Equal(string.Empty, fullResponseBody);
            }
        }
        
        [Fact]
        public async Task ValidateNoContent()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.UseBouncyCastleSslEngine();

            setting.ConfigureRule().WhenAny().Do(new SkipRemoteCertificateValidationAction());
            
            await using var proxy = new Proxy(setting);

            using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);

            var request = new HttpRequestMessage(HttpMethod.Get,
                "https://sdkweb.pvid.ariadnext.io/assets/images/selfie_challenge_center.mp4?v=1");

            // Ajout des en-têtes
            request.Headers.TryAddWithoutValidation("sec-ch-ua-platform", "\"Android\"");
            request.Headers.TryAddWithoutValidation("Accept-Encoding", "identity;q=1, *;q=0");
            request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Mobile Safari/537.36");
            request.Headers.TryAddWithoutValidation("sec-ch-ua", "\"Google Chrome\";v=\"131\", \"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\"");
            request.Headers.TryAddWithoutValidation("sec-ch-ua-mobile", "?1");
            request.Headers.TryAddWithoutValidation("Accept", "*/*");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "same-origin");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "cors");
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "video");
            request.Headers.TryAddWithoutValidation("Referer", "https://sdkweb.pvid.ariadnext.io/v2/my-sg/ea0b115d-1cb6-4eca-9b75-840a52dd0b12");
            request.Headers.TryAddWithoutValidation("Accept-Language", "fr-FR,fr;q=0.9");
            request.Headers.TryAddWithoutValidation("Range", "bytes=0-");

            using var response = await client.SendAsync(request);

            var contentStream = await response.Content.ReadAsStreamAsync();

            var length = await contentStream.DrainAsync();
            var statusCode = (int)response.StatusCode;

            Assert.Equal(206, statusCode);
        }
    }

}
