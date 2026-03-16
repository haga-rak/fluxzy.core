using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fluxzy.Tests._Fixtures;

/// <summary>
///     A simple in-process Kestrel HTTPS server for integration testing.
/// </summary>
public class InProcessHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    public int Port { get; }

    public string BaseUrl => $"https://localhost:{Port}";

    private InProcessHost(WebApplication app, int port)
    {
        _app = app;
        Port = port;
    }

    public static async Task<InProcessHost> Create(
        Action<WebApplication>? configureRoutes = null)
    {
        var certificate = CreateSelfSignedCertificate();

        var builder = WebApplication.CreateBuilder();

        builder.WebHost.ConfigureKestrel(k =>
        {
            k.Listen(IPAddress.Loopback, 0, listenOptions =>
            {
                listenOptions.UseHttps(certificate);
            });
        });

        var app = builder.Build();

        if (configureRoutes != null)
        {
            configureRoutes(app);
        }
        else
        {
            ConfigureDefaultRoutes(app);
        }

        await app.StartAsync();

        var server = app.Services.GetRequiredService<IServer>();
        var addressFeature = server.Features.Get<IServerAddressesFeature>()!;
        var port = addressFeature.Addresses
            .Select(a => new Uri(a))
            .First()
            .Port;

        return new InProcessHost(app, port);
    }

    private static void ConfigureDefaultRoutes(WebApplication app)
    {
        app.MapGet("/hello", () => Results.Ok(new { message = "Hello from Kestrel!" }));

        app.MapGet("/echo", (HttpContext ctx) =>
        {
            var headers = ctx.Request.Headers
                .ToDictionary(h => h.Key, h => h.Value.ToString());

            return Results.Ok(new { headers });
        });
    }

    private static X509Certificate2 CreateSelfSignedCertificate()
    {
        using var rsa = RSA.Create(2048);

        var request = new CertificateRequest(
            "CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false));

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(sanBuilder.Build());

        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddYears(1));

        return new X509Certificate2(
            certificate.Export(X509ContentType.Pfx));
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
