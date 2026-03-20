// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Tests._Fixtures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Fluxzy.Tests.UnitTests.H2Serve
{
    public class ServeHttp11ActionTests
    {
        /// <summary>
        ///     When ServeH2 is enabled globally but ServeHttp11Action is applied,
        ///     the client should receive an HTTP/1.1 response.
        /// </summary>
        [Fact]
        public async Task ServeHttp11Action_OverridesGlobalServeH2()
        {
            var host = await InProcessHost.Create(app =>
            {
                app.MapGet("/test", async (HttpContext ctx) =>
                {
                    ctx.Response.ContentType = "text/plain";
                    await ctx.Response.WriteAsync("OK");
                });
            });

            await using var _ = host;

            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.SetServeH2(true);

            setting.AddAlterationRules(new SkipRemoteCertificateValidationAction(), AnyFilter.Default);

            setting.ConfigureRule()
                   .WhenAny()
                   .Do(new ServeHttp11Action());

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();
            var proxyEndPoint = endPoints.First();

            // Client prefers H2 but will accept H1.1 if server only offers it
            using var client = Socks5ClientFactory.Create(proxyEndPoint);
            client.BaseAddress = new Uri(host.BaseUrl);
            client.DefaultRequestVersion = new Version(2, 0);
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

            var response = await client.GetAsync("/test");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new Version(1, 1), response.Version);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("OK", body);
        }

        /// <summary>
        ///     When ServeH2 is enabled and ServeHttp11Action is NOT applied,
        ///     the client should receive an HTTP/2 response (control test).
        /// </summary>
        [Fact]
        public async Task WithoutServeHttp11Action_ClientGetsH2()
        {
            await using var setup = await ProxiedHostSetup.Create(
                configureSetting: setting =>
                {
                    setting.SetServeH2(true);
                },
                configureRoutes: app =>
                {
                    app.MapGet("/test", async (HttpContext ctx) =>
                    {
                        ctx.Response.ContentType = "text/plain";
                        await ctx.Response.WriteAsync("OK");
                    });
                },
                httpVersion: new Version(2, 0));

            var response = await setup.Client.GetAsync("/test");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new Version(2, 0), response.Version);
        }

        /// <summary>
        ///     When ServeHttp11Action is applied per-host filter, only matching hosts should be downgraded.
        /// </summary>
        [Fact]
        public async Task ServeHttp11Action_AppliedPerHost()
        {
            var host = await InProcessHost.Create(app =>
            {
                app.MapGet("/test", async (HttpContext ctx) =>
                {
                    ctx.Response.ContentType = "text/plain";
                    await ctx.Response.WriteAsync("OK");
                });
            });

            await using var _ = host;

            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.SetServeH2(true);

            setting.AddAlterationRules(new SkipRemoteCertificateValidationAction(), AnyFilter.Default);

            setting.ConfigureRule()
                   .When(new HostFilter("localhost"))
                   .Do(new ServeHttp11Action());

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();
            var proxyEndPoint = endPoints.First();

            using var client = Socks5ClientFactory.Create(proxyEndPoint);
            client.BaseAddress = new Uri(host.BaseUrl);
            client.DefaultRequestVersion = new Version(2, 0);
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

            var response = await client.GetAsync("/test");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new Version(1, 1), response.Version);
        }
    }
}
