using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder();
builder.Logging.ClearProviders();

var certificate = CreateSelfSignedCertificate();

builder.WebHost.ConfigureKestrel(k =>
{
    k.Listen(IPAddress.Loopback, 0, listenOptions =>
    {
        listenOptions.UseHttps(certificate);
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
});

var app = builder.Build();

app.MapGet("/bench", async ctx =>
{
    var lengthStr = ctx.Request.Query["length"].FirstOrDefault();
    var length = lengthStr != null ? int.Parse(lengthStr) : 0;

    if (length > 0)
    {
        ctx.Response.ContentLength = length;
        var buffer = new byte[Math.Min(length, 16384)];
        var remaining = length;

        while (remaining > 0)
        {
            var toWrite = Math.Min(remaining, buffer.Length);
            await ctx.Response.Body.WriteAsync(buffer.AsMemory(0, toWrite));
            remaining -= toWrite;
        }
    }
});

await app.StartAsync();

var server = app.Services.GetRequiredService<IServer>();
var addressFeature = server.Features.Get<IServerAddressesFeature>()!;
var port = addressFeature.Addresses.Select(a => new Uri(a)).First().Port;

Console.WriteLine($"LISTENING:{port}");
Console.Out.Flush();

// Wait for stdin close (parent process exit) or SIGTERM
await Console.In.ReadLineAsync();
await app.StopAsync();

static X509Certificate2 CreateSelfSignedCertificate()
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

    return new X509Certificate2(certificate.Export(X509ContentType.Pfx));
}
