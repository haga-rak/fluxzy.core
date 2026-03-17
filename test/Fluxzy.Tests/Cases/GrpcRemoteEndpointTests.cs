using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Xunit;

namespace Fluxzy.Tests.Cases;

/// <summary>
/// Integration tests that use grpcurl to call remote gRPC endpoints (grpcb.in)
/// through a Fluxzy proxy in tunnel mode (SkipSslTunneling).
/// </summary>
public class GrpcRemoteEndpointTests
{
    private const string GrpcBinHost = "grpcb.in:9001";

    private static async Task<(string stdout, string stderr, int exitCode)> RunGrpcurl(
        string proxyAddress, string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "grpcurl",
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.Environment["https_proxy"] = proxyAddress;

        using var process = Process.Start(psi)!;

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return (stdout, stderr, process.ExitCode);
    }

    private static async Task<(Proxy proxy, string proxyUrl)> CreateProxy(
        Action<FluxzySetting>? configure = null)
    {
        var setting = FluxzySetting.CreateLocalRandomPort();
        setting.SetServeH2(true);

        // Skip SSL tunneling: the proxy acts as a transparent TCP tunnel for TLS,
        // letting grpcurl negotiate H2/TLS directly with grpcb.in.
        setting.AddAlterationRules(
            new SkipSslTunnelingAction(), AnyFilter.Default);

        configure?.Invoke(setting);

        var proxy = new Proxy(setting);
        var endpoint = proxy.Run().First();

        if (endpoint.Address.Equals(IPAddress.Any))
            endpoint = new IPEndPoint(IPAddress.Loopback, endpoint.Port);
        else if (endpoint.Address.Equals(IPAddress.IPv6Any))
            endpoint = new IPEndPoint(IPAddress.IPv6Loopback, endpoint.Port);

        var proxyUrl = $"http://{endpoint.Address}:{endpoint.Port}";

        return (proxy, proxyUrl);
    }

    [Fact]
    public async Task Unary_SayHello_Through_Proxy()
    {
        var (proxy, proxyUrl) = await CreateProxy();

        await using (proxy)
        {
            var (stdout, stderr, exitCode) = await RunGrpcurl(proxyUrl,
                $"-insecure -d \"{{\\\"greeting\\\": \\\"fluxzy\\\"}}\" {GrpcBinHost} hello.HelloService/SayHello");

            Assert.True(exitCode == 0, $"grpcurl failed with exit code {exitCode}. stderr: {stderr}");

            using var doc = JsonDocument.Parse(stdout);
            var reply = doc.RootElement.GetProperty("reply").GetString();

            Assert.Equal("hello fluxzy", reply);
        }
    }

    [Fact]
    public async Task Unary_DummyUnary_Through_Proxy()
    {
        var (proxy, proxyUrl) = await CreateProxy();

        await using (proxy)
        {
            var (stdout, stderr, exitCode) = await RunGrpcurl(proxyUrl,
                $"-insecure -d \"{{\\\"f_string\\\": \\\"test-value\\\"}}\" {GrpcBinHost} grpcbin.GRPCBin/DummyUnary");

            Assert.True(exitCode == 0, $"grpcurl failed with exit code {exitCode}. stderr: {stderr}");

            using var doc = JsonDocument.Parse(stdout);
            var fString = doc.RootElement.GetProperty("fString").GetString();

            Assert.Equal("test-value", fString);
        }
    }

    [Fact]
    public async Task Empty_Call_Through_Proxy()
    {
        var (proxy, proxyUrl) = await CreateProxy();

        await using (proxy)
        {
            var (stdout, stderr, exitCode) = await RunGrpcurl(proxyUrl,
                $"-insecure {GrpcBinHost} grpcbin.GRPCBin/Empty");

            Assert.True(exitCode == 0, $"grpcurl failed with exit code {exitCode}. stderr: {stderr}");

            // Empty call returns an empty JSON object
            var trimmed = stdout.Trim();
            Assert.True(trimmed == "{}" || trimmed == "{\n\n}" || trimmed == "{\r\n\r\n}",
                $"Expected empty response but got: {trimmed}");
        }
    }

    [Fact]
    public async Task Index_Call_Through_Proxy()
    {
        var (proxy, proxyUrl) = await CreateProxy();

        await using (proxy)
        {
            var (stdout, stderr, exitCode) = await RunGrpcurl(proxyUrl,
                $"-insecure {GrpcBinHost} grpcbin.GRPCBin/Index");

            Assert.True(exitCode == 0, $"grpcurl failed with exit code {exitCode}. stderr: {stderr}");

            using var doc = JsonDocument.Parse(stdout);

            Assert.True(doc.RootElement.TryGetProperty("endpoints", out _),
                $"Expected 'endpoints' in response. Got: {stdout}");
        }
    }

    [Fact]
    public async Task ServerStreaming_LotsOfReplies_Through_Proxy()
    {
        var (proxy, proxyUrl) = await CreateProxy();

        await using (proxy)
        {
            var (stdout, stderr, exitCode) = await RunGrpcurl(proxyUrl,
                $"-insecure -d \"{{\\\"greeting\\\": \\\"stream-test\\\"}}\" {GrpcBinHost} hello.HelloService/LotsOfReplies");

            Assert.True(exitCode == 0, $"grpcurl failed with exit code {exitCode}. stderr: {stderr}");

            // Server streaming returns multiple JSON objects containing the greeting
            Assert.Contains("stream-test", stdout);
        }
    }

    [Fact]
    public async Task SpecificError_Returns_Expected_StatusCode_Through_Proxy()
    {
        var (proxy, proxyUrl) = await CreateProxy();

        await using (proxy)
        {
            // Request a specific gRPC error code (NOT_FOUND = 5)
            var (stdout, stderr, exitCode) = await RunGrpcurl(proxyUrl,
                $"-insecure -d \"{{\\\"code\\\": 5}}\" {GrpcBinHost} grpcbin.GRPCBin/SpecificError");

            // grpcurl returns non-zero exit code on gRPC errors
            Assert.NotEqual(0, exitCode);
            Assert.Contains("NotFound", stderr);
        }
    }

    [Fact]
    public async Task List_Services_Through_Proxy()
    {
        var (proxy, proxyUrl) = await CreateProxy();

        await using (proxy)
        {
            var (stdout, stderr, exitCode) = await RunGrpcurl(proxyUrl,
                $"-insecure {GrpcBinHost} list");

            Assert.True(exitCode == 0, $"grpcurl list failed. stderr: {stderr}");
            Assert.Contains("hello.HelloService", stdout);
            Assert.Contains("grpcbin.GRPCBin", stdout);
        }
    }

    [Fact]
    public async Task Describe_Service_Through_Proxy()
    {
        var (proxy, proxyUrl) = await CreateProxy();

        await using (proxy)
        {
            var (stdout, stderr, exitCode) = await RunGrpcurl(proxyUrl,
                $"-insecure {GrpcBinHost} describe hello.HelloService");

            Assert.True(exitCode == 0, $"grpcurl describe failed. stderr: {stderr}");
            Assert.Contains("SayHello", stdout);
            Assert.Contains("LotsOfReplies", stdout);
        }
    }
}
