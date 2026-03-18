// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Fluxzy.Tests.UnitTests.H2Serve
{
    public class H2ServeTests
    {

        [Fact]
        public async Task SimpleH2Request()
        {
            var fluxzySetting = FluxzySetting.CreateLocalRandomPort();

            fluxzySetting.SetServeH2(true);

            await using var proxy = new Proxy(fluxzySetting);
            var endPoints = proxy.Run();

            var client = HttpClientUtility.CreateHttpClient(endPoints, fluxzySetting);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get,
                TestConstants.Http2Host + "/ip");

            using var response = await client.SendAsync(requestMessage);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// When the upstream server sends a response without content-length (common with H2 servers),
        /// the proxy must NOT add transfer-encoding: chunked to the H2 downstream response.
        /// HTTP/2 forbids transfer-encoding (RFC 7540 §8.1.2.2).
        /// </summary>
        [Fact]
        public async Task H2Downstream_NoTransferEncodingOnStreamedResponse()
        {
            await using var setup = await ProxiedHostSetup.Create(
                configureSetting: setting => setting.SetServeH2(true),
                configureRoutes: app =>
                {
                    // This route writes a response body without setting content-length,
                    // causing the upstream to omit it. The proxy previously added
                    // transfer-encoding: chunked which is illegal in H2.
                    app.MapGet("/streamed", async (HttpContext ctx) =>
                    {
                        ctx.Response.ContentType = "text/plain";
                        await ctx.Response.WriteAsync("Hello streamed world");
                    });
                },
                httpVersion: new Version(2, 0));

            var response = await setup.Client.GetAsync("/streamed");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new Version(2, 0), response.Version);

            // Verify no transfer-encoding header in the H2 response
            Assert.False(
                response.Headers.Contains("Transfer-Encoding"),
                "H2 response must not contain transfer-encoding header");

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello streamed world", body);
        }

        [Theory]
        [InlineData(2000, 0)]
        [InlineData(4000, 0)]
        [InlineData(8000, 0)]
        [InlineData(2000, 4000)]
        [InlineData(4000, 4000)]
        public async Task H2Downstream_LongHeaders(int queryLength, int cookieLength)
        {
            var longQuery = "bidder=loopme&gdpr=1&gdpr_consent=" + new string('A', queryLength)
                + "&gpp=DBAA&gpp_sid=-1&f=i&uid=a4a83c9b-812e-4acf-915e-d910fdf0788f";

            var cookie = cookieLength > 0
                ? "EuConsent=" + new string('B', cookieLength)
                : null;

            await using var setup = await ProxiedHostSetup.Create(
                configureSetting: setting => setting.SetServeH2(true),
                configureRoutes: app =>
                {
                    app.MapGet("/setuid", async (HttpContext ctx) =>
                    {
                        ctx.Response.ContentType = "text/plain";
                        await ctx.Response.WriteAsync("OK");
                    });
                },
                httpVersion: new Version(2, 0));

            if (cookie != null)
                setup.Client.DefaultRequestHeaders.Add("Cookie", cookie);

            var response = await setup.Client.GetAsync($"/setuid?{longQuery}");

            var body = await response.Content.ReadAsStringAsync();

            Assert.True(response.StatusCode == HttpStatusCode.OK,
                $"Expected OK but got {response.StatusCode}. Body: {body}");
            Assert.Equal(new Version(2, 0), response.Version);
            Assert.Equal("OK", body);
        }
        /// <summary>
        /// Exercises Http11PoolProcessing.Process with headers that exceed the default
        /// 4 KB RsBuffer, ensuring the buffer is resized before writing HTTP/1.1 headers upstream.
        /// </summary>
        [Theory]
        [InlineData(4000, 0)]
        [InlineData(2000, 4000)]
        public async Task Http11Upstream_LongHeaders(int queryLength, int cookieLength)
        {
            var longQuery = "bidder=loopme&gdpr=1&gdpr_consent=" + new string('A', queryLength)
                + "&gpp=DBAA&gpp_sid=-1&f=i&uid=a4a83c9b-812e-4acf-915e-d910fdf0788f";

            var cookie = cookieLength > 0
                ? "EuConsent=" + new string('B', cookieLength)
                : null;

            await using var setup = await ProxiedHostSetup.Create(
                configureSetting: setting => setting.SetServeH2(false),
                configureRoutes: app =>
                {
                    app.MapGet("/setuid", async (HttpContext ctx) =>
                    {
                        ctx.Response.ContentType = "text/plain";
                        await ctx.Response.WriteAsync("OK");
                    });
                });

            if (cookie != null)
                setup.Client.DefaultRequestHeaders.Add("Cookie", cookie);

            var response = await setup.Client.GetAsync($"/setuid?{longQuery}");

            var body = await response.Content.ReadAsStringAsync();

            Assert.True(response.StatusCode == HttpStatusCode.OK,
                $"Expected OK but got {response.StatusCode}. Body: {body}");
            Assert.Equal("OK", body);
        }
    }
}
