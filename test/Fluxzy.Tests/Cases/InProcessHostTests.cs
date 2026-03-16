using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Tests._Fixtures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Fluxzy.Tests.Cases;

public class InProcessHostTests
{
    [Fact]
    public async Task Get_Hello_Direct_Returns_Expected_Message()
    {
        await using var host = await InProcessHost.Create();

        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        using var client = new HttpClient(handler) { BaseAddress = new Uri(host.BaseUrl) };

        var response = await client.GetAsync("/hello");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Hello from Kestrel!", doc.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Get_Hello_Through_Fluxzy_Socks5_Returns_Expected_Message()
    {
        await using var setup = await ProxiedHostSetup.Create(setting =>
        {
            setting.AddAlterationRules(
                new AddResponseHeaderAction("X-Fluxzy-Proxied", "true"),
                AnyFilter.Default);
        });

        var response = await setup.Client.GetAsync("/hello");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Hello from Kestrel!", doc.RootElement.GetProperty("message").GetString());

        Assert.True(response.Headers.TryGetValues("X-Fluxzy-Proxied", out var values));
        Assert.Equal("true", values!.First());
    }
}

public class InProcessHostH2Tests
{
    [Fact]
    public async Task Get_Hello_Through_Fluxzy_Socks5_With_H2_Downstream()
    {
        await using var host = await InProcessHost.Create();

        var setting = FluxzySetting.CreateLocalRandomPort();
        setting.SetServeH2(true);
        setting.AddAlterationRules(
            new SkipRemoteCertificateValidationAction(), AnyFilter.Default);
        setting.AddAlterationRules(
            new AddResponseHeaderAction("X-Fluxzy-H2", "true"), AnyFilter.Default);

        await using var proxy = new Proxy(setting);
        var proxyEndPoint = proxy.Run().First();

        using var client = Socks5ClientFactory.Create(proxyEndPoint, httpVersion: new Version(2, 0));
        client.BaseAddress = new Uri(host.BaseUrl);

        var response = await client.GetAsync("/hello");

        // Confirm the client-proxy connection used HTTP/2
        Assert.Equal(new Version(2, 0), response.Version);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Hello from Kestrel!", doc.RootElement.GetProperty("message").GetString());

        Assert.True(response.Headers.TryGetValues("X-Fluxzy-H2", out var values));
        Assert.Equal("true", values!.First());
    }
}

public class InProcessHostTrailerTests
{
    /// <summary>
    /// Full integration test: Kestrel sends response trailers over HTTP/2,
    /// Fluxzy proxies them, client receives them via HttpResponseMessage.TrailingHeaders.
    /// </summary>
    [Fact]
    public async Task ResponseTrailers_ForwardedThroughProxy_H2()
    {
        // ProxiedHostSetup.Create creates InProcessHost + Fluxzy Proxy + HttpClient
        await using var setup = await ProxiedHostSetup.Create(
            configureSetting: setting => setting.SetServeH2(true),
            configureRoutes: app =>
            {
                app.MapPost("/with-trailers", async (HttpContext ctx) =>
                {
                    ctx.Response.DeclareTrailer("grpc-status");
                    ctx.Response.DeclareTrailer("grpc-message");

                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "text/plain";

                    await ctx.Response.WriteAsync("Hello with trailers");
                    await ctx.Response.Body.FlushAsync();

                    ctx.Response.AppendTrailer("grpc-status", "0");
                    ctx.Response.AppendTrailer("grpc-message", "OK");
                });
            },
            httpVersion: new Version(2, 0));

        // Send POST request
        var content = new StringContent("test body", Encoding.UTF8, "application/grpc");
        var response = await setup.Client.PostAsync("/with-trailers", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new Version(2, 0), response.Version);

        // Read body (must be read before trailing headers are accessible)
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Hello with trailers", body);

        // Verify trailing headers arrived through the proxy
        Assert.True(response.TrailingHeaders.TryGetValues("grpc-status", out var grpcStatus),
            "grpc-status trailer should be present");
        Assert.Equal("0", grpcStatus!.First());

        Assert.True(response.TrailingHeaders.TryGetValues("grpc-message", out var grpcMessage),
            "grpc-message trailer should be present");
        Assert.Equal("OK", grpcMessage!.First());
    }

    /// <summary>
    /// Integration test: response without trailers still works correctly with H2.
    /// </summary>
    [Fact]
    public async Task ResponseWithoutTrailers_StillWorks_H2()
    {
        await using var setup = await ProxiedHostSetup.Create(
            configureSetting: setting => setting.SetServeH2(true),
            httpVersion: new Version(2, 0));

        var response = await setup.Client.GetAsync("/hello");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new Version(2, 0), response.Version);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Hello from Kestrel!", doc.RootElement.GetProperty("message").GetString());

        // No trailing headers should be present
        Assert.Empty(response.TrailingHeaders);
    }

    /// <summary>
    /// Integration test: Kestrel sends a single trailer field, proxied through H2.
    /// </summary>
    [Fact]
    public async Task SingleResponseTrailer_ForwardedCorrectly_H2()
    {
        await using var setup = await ProxiedHostSetup.Create(
            configureSetting: setting => setting.SetServeH2(true),
            configureRoutes: app =>
            {
                app.MapGet("/single-trailer", async (HttpContext ctx) =>
                {
                    ctx.Response.DeclareTrailer("x-checksum");
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/octet-stream";

                    await ctx.Response.Body.WriteAsync(new byte[] { 1, 2, 3, 4 });
                    await ctx.Response.Body.FlushAsync();

                    ctx.Response.AppendTrailer("x-checksum", "a1b2c3d4");
                });
            },
            httpVersion: new Version(2, 0));

        var response = await setup.Client.GetAsync("/single-trailer");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Read body first
        var bodyBytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, bodyBytes);

        // Check trailer
        Assert.True(response.TrailingHeaders.TryGetValues("x-checksum", out var checksum));
        Assert.Equal("a1b2c3d4", checksum!.First());
    }
}
