// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Fluxzy.Tests._Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace Fluxzy.Tests.Cases
{
    public class Socks5H2YahooFinanceTests
    {
        private const string CurlPath =
            @"C:\Users\haga\AppData\Local\Microsoft\WinGet\Packages\cURL.cURL_Microsoft.Winget.Source_8wekyb3d8bbwe\curl-8.19.0_2-win64-mingw\bin\curl.exe";

        private const string TargetUrl = "https://fr.finance.yahoo.com/research-hub/screener/";

        private static readonly string[] BrowserHeaders = new[]
        {
            "user-agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:148.0) Gecko/20100101 Firefox/148.0",
            "accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
            "accept-language: fr,fr-FR;q=0.9,en-US;q=0.8,en;q=0.7",
            "accept-encoding: gzip, deflate, br, zstd",
            "referer: https://fr.finance.yahoo.com/",
            "upgrade-insecure-requests: 1",
            "sec-fetch-dest: document",
            "sec-fetch-mode: navigate",
            "sec-fetch-site: same-origin",
            "sec-fetch-user: ?1",
            "priority: u=0, i",
            "te: trailers"
        };

        private readonly ITestOutputHelper _output;

        public Socks5H2YahooFinanceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        ///     Reproduces HTTP 528 error on https://fr.finance.yahoo.com/research-hub/screener/
        ///     with browser-like headers through HTTP CONNECT + H2 (H2DownStreamPipe).
        /// </summary>
        [Fact]
        public async Task HttpConnect_H2_YahooScreener_BrowserHeaders_ShouldNotReturn528()
        {
            // Arrange
            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.SetServeH2(true);

            setting.AddAlterationRules(
                new SkipRemoteCertificateValidationAction(),
                AnyFilter.Default
            );

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();
            var proxyEndPoint = NormalizeEndPoint(endPoints.First());

            // Act
            var (statusCode, httpVersion, body, stderr) = await RunCurl(
                proxyEndPoint, TargetUrl, http2: true, headers: BrowserHeaders, verbose: true);

            // Assert
            _output.WriteLine($"Status code: {statusCode}");
            _output.WriteLine($"HTTP version: {httpVersion}");
            _output.WriteLine($"Body length: {body.Length}");
            _output.WriteLine($"Verbose:\n{stderr}");

            if (statusCode == 528)
                _output.WriteLine($"528 body (truncated):\n{body.Substring(0, Math.Min(body.Length, 2000))}");

            Assert.NotEqual(528, statusCode);
        }

        /// <summary>
        ///     Same test with exchange capture to inspect error details.
        /// </summary>
        [Fact]
        public async Task HttpConnect_H2_YahooScreener_CaptureExchangeDetails()
        {
            // Arrange
            await using var proxy = new AddHocConfigurableProxy(
                expectedRequestCount: 1,
                timeoutSeconds: 30,
                configureSetting: setting =>
                {
                    setting.SetServeH2(true);
                    setting.AddAlterationRules(
                        new SkipRemoteCertificateValidationAction(),
                        AnyFilter.Default
                    );
                });

            var endPoints = proxy.Run();
            var proxyEndPoint = NormalizeEndPoint(endPoints.First());

            // Act
            var (statusCode, httpVersion, body, stderr) = await RunCurl(
                proxyEndPoint, TargetUrl, http2: true, headers: BrowserHeaders, verbose: true);

            _output.WriteLine($"Status code: {statusCode}");
            _output.WriteLine($"HTTP version: {httpVersion}");
            _output.WriteLine($"Verbose:\n{stderr}");

            // Wait for exchange capture
            try
            {
                await proxy.WaitUntilDone();
            }
            catch (TimeoutException)
            {
                _output.WriteLine("Timed out waiting for exchange capture");
            }

            foreach (var exchange in proxy.CapturedExchanges)
            {
                _output.WriteLine($"Exchange {exchange.Id}:");
                _output.WriteLine($"  Authority: {exchange.Authority}");
                _output.WriteLine($"  Status: {exchange.StatusCode}");
                _output.WriteLine($"  HttpVersion: {exchange.HttpVersion}");

                if (exchange.ClientErrors.Any())
                {
                    _output.WriteLine($"  Client Errors:");
                    foreach (var error in exchange.ClientErrors)
                    {
                        _output.WriteLine($"    Code: {error.ErrorCode}, Message: {error.Message}");
                        if (!string.IsNullOrEmpty(error.ExceptionMessage))
                            _output.WriteLine($"    Exception: {error.ExceptionMessage}");
                    }
                }
            }

            if (statusCode == 528)
                _output.WriteLine($"528 body (truncated):\n{body.Substring(0, Math.Min(body.Length, 2000))}");

            Assert.NotEqual(528, statusCode);
        }

        /// <summary>
        ///     Control: same URL + headers but forced HTTP/1.1 (no H2DownStreamPipe).
        /// </summary>
        [Fact]
        public async Task HttpConnect_H1_YahooScreener_BrowserHeaders_ShouldNotReturn528()
        {
            // Arrange
            var setting = FluxzySetting.CreateLocalRandomPort();

            setting.AddAlterationRules(
                new SkipRemoteCertificateValidationAction(),
                AnyFilter.Default
            );

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();
            var proxyEndPoint = NormalizeEndPoint(endPoints.First());

            // Act - force H1
            var (statusCode, httpVersion, body, stderr) = await RunCurl(
                proxyEndPoint, TargetUrl, http2: false, headers: BrowserHeaders, verbose: true);

            // Assert
            _output.WriteLine($"Status code (H1): {statusCode}");
            _output.WriteLine($"HTTP version: {httpVersion}");
            _output.WriteLine($"Body length: {body.Length}");
            _output.WriteLine($"Verbose:\n{stderr}");

            Assert.NotEqual(528, statusCode);
        }

        /// <summary>
        ///     H2 without browser headers — isolates whether headers matter.
        /// </summary>
        [Fact]
        public async Task HttpConnect_H2_YahooScreener_NoExtraHeaders_ShouldNotReturn528()
        {
            // Arrange
            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.SetServeH2(true);

            setting.AddAlterationRules(
                new SkipRemoteCertificateValidationAction(),
                AnyFilter.Default
            );

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();
            var proxyEndPoint = NormalizeEndPoint(endPoints.First());

            // Act — bare curl, no browser headers
            var (statusCode, httpVersion, body, stderr) = await RunCurl(
                proxyEndPoint, TargetUrl, http2: true, verbose: true);

            // Assert
            _output.WriteLine($"Status code (no extra headers): {statusCode}");
            _output.WriteLine($"HTTP version: {httpVersion}");
            _output.WriteLine($"Body length: {body.Length}");
            _output.WriteLine($"Verbose:\n{stderr}");

            if (statusCode == 528)
                _output.WriteLine($"528 body (truncated):\n{body.Substring(0, Math.Min(body.Length, 2000))}");

            Assert.NotEqual(528, statusCode);
        }

        private static async Task<(int statusCode, string httpVersion, string body, string stderr)> RunCurl(
            IPEndPoint proxyEndPoint, string url, bool http2,
            string[]? headers = null, bool verbose = false)
        {
            const string separator = "\n---CURL_META---\n";
            var writeOut = $"{separator}%{{http_code}}\\n%{{http_version}}";

            var args = $"--proxy http://{proxyEndPoint} -k --max-time 30 --compressed";

            if (http2)
                args += " --http2";
            else
                args += " --http1.1";

            if (verbose)
                args += " -v";

            if (headers != null)
            {
                foreach (var header in headers)
                    args += $" -H \"{header}\"";
            }

            args += $" -w \"{writeOut}\" \"{url}\"";

            var psi = new ProcessStartInfo
            {
                FileName = CurlPath,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi)!;

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            var sepIndex = stdout.LastIndexOf(separator.TrimEnd('\n'));
            string body;
            int statusCode = 0;
            string httpVersion = "unknown";

            if (sepIndex >= 0)
            {
                body = stdout.Substring(0, sepIndex);
                var meta = stdout.Substring(sepIndex + separator.Length).Trim();
                var metaLines = meta.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                if (metaLines.Length >= 1)
                    int.TryParse(metaLines[0].Trim(), out statusCode);

                if (metaLines.Length >= 2)
                    httpVersion = metaLines[1].Trim();
            }
            else
            {
                body = stdout;
            }

            return (statusCode, httpVersion, body, stderr);
        }

        /// <summary>
        ///     Isolate which header causes the 528 by testing without te:trailers.
        /// </summary>
        [Fact]
        public async Task HttpConnect_H2_YahooScreener_WithoutTeTrailers_ShouldNotReturn528()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.SetServeH2(true);
            setting.AddAlterationRules(
                new SkipRemoteCertificateValidationAction(), AnyFilter.Default);

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();
            var proxyEndPoint = NormalizeEndPoint(endPoints.First());

            // All browser headers EXCEPT te:trailers
            var headersNoTe = BrowserHeaders.Where(h => !h.StartsWith("te:")).ToArray();

            var (statusCode, httpVersion, body, stderr) = await RunCurl(
                proxyEndPoint, TargetUrl, http2: true, headers: headersNoTe, verbose: true);

            _output.WriteLine($"Status code (no te:trailers): {statusCode}");
            _output.WriteLine($"HTTP version: {httpVersion}");
            _output.WriteLine($"Body length: {body.Length}");
            _output.WriteLine($"Verbose:\n{stderr}");

            if (statusCode == 528)
                _output.WriteLine($"528 body:\n{body.Substring(0, Math.Min(body.Length, 2000))}");

            Assert.NotEqual(528, statusCode);
        }

        /// <summary>
        ///     Test with ONLY te:trailers header to confirm it's the trigger.
        /// </summary>
        [Fact]
        public async Task HttpConnect_H2_YahooScreener_OnlyTeTrailers_ShouldNotReturn528()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.SetServeH2(true);
            setting.AddAlterationRules(
                new SkipRemoteCertificateValidationAction(), AnyFilter.Default);

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();
            var proxyEndPoint = NormalizeEndPoint(endPoints.First());

            // Only add te:trailers + minimal browser UA
            var minimalHeaders = new[]
            {
                "user-agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:148.0) Gecko/20100101 Firefox/148.0",
                "accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
                "accept-encoding: gzip, deflate, br, zstd",
                "te: trailers"
            };

            var (statusCode, httpVersion, body, stderr) = await RunCurl(
                proxyEndPoint, TargetUrl, http2: true, headers: minimalHeaders, verbose: true);

            _output.WriteLine($"Status code (te:trailers only): {statusCode}");
            _output.WriteLine($"HTTP version: {httpVersion}");
            _output.WriteLine($"Body length: {body.Length}");
            _output.WriteLine($"Verbose:\n{stderr}");

            if (statusCode == 528)
                _output.WriteLine($"528 body:\n{body.Substring(0, Math.Min(body.Length, 2000))}");

            Assert.NotEqual(528, statusCode);
        }

        /// <summary>
        ///     Test without accept-encoding to isolate compression-related issues.
        /// </summary>
        [Fact]
        public async Task HttpConnect_H2_YahooScreener_WithoutAcceptEncoding_ShouldNotReturn528()
        {
            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.SetServeH2(true);
            setting.AddAlterationRules(
                new SkipRemoteCertificateValidationAction(), AnyFilter.Default);

            await using var proxy = new Proxy(setting);
            var endPoints = proxy.Run();
            var proxyEndPoint = NormalizeEndPoint(endPoints.First());

            var headersNoEncoding = BrowserHeaders
                .Where(h => !h.StartsWith("accept-encoding:"))
                .ToArray();

            var (statusCode, httpVersion, body, stderr) = await RunCurl(
                proxyEndPoint, TargetUrl, http2: true, headers: headersNoEncoding, verbose: true);

            _output.WriteLine($"Status code (no accept-encoding): {statusCode}");
            _output.WriteLine($"HTTP version: {httpVersion}");
            _output.WriteLine($"Body length: {body.Length}");
            _output.WriteLine($"Verbose:\n{stderr}");

            if (statusCode == 528)
                _output.WriteLine($"528 body:\n{body.Substring(0, Math.Min(body.Length, 2000))}");

            Assert.NotEqual(528, statusCode);
        }

        private static IPEndPoint NormalizeEndPoint(IPEndPoint endPoint)
        {
            if (endPoint.Address.Equals(IPAddress.Any))
                return new IPEndPoint(IPAddress.Loopback, endPoint.Port);

            if (endPoint.Address.Equals(IPAddress.IPv6Any))
                return new IPEndPoint(IPAddress.IPv6Loopback, endPoint.Port);

            return endPoint;
        }
    }
}
