using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Fluxzy.Tests._Fixtures;

public class GrpcInProcessHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    public int Port { get; }

    public string BaseUrl => $"https://localhost:{Port}";

    private GrpcInProcessHost(WebApplication app, int port)
    {
        _app = app;
        Port = port;
    }

    public static async Task<GrpcInProcessHost> Create()
    {
        var certificate = CreateSelfSignedCertificate();

        var builder = WebApplication.CreateBuilder();

        builder.Services.AddGrpc();

        builder.WebHost.ConfigureKestrel(k =>
        {
            k.Listen(IPAddress.Loopback, 0, listenOptions =>
            {
                listenOptions.UseHttps(certificate);
                listenOptions.Protocols = HttpProtocols.Http2;
            });
        });

        var app = builder.Build();

        app.MapGrpcService<GreeterServiceImpl>();

        await app.StartAsync();

        var server = app.Services.GetRequiredService<IServer>();
        var addressFeature = server.Features.Get<IServerAddressesFeature>()!;
        var port = addressFeature.Addresses
            .Select(a => new Uri(a))
            .First()
            .Port;

        return new GrpcInProcessHost(app, port);
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
