using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Tests._Fixtures;
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
