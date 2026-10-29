// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Archiving;
using Fluxzy.Core;
using Fluxzy.Core.Pcap;
using Fluxzy.Core.Pcap.Cli.Clients;
using Fluxzy.Readers;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    [Collection("RUNS_RAW_CAPTURE")]
    public class CurlFaviconRawCaptureTests
    {
        [Fact]
        public async Task Curl_Favicon_With_Pcap_Extracts_Capture_Next_To_Fxzy()
        {
            var outDirectory = Path.Combine("Drop", $"curl-favicon-capture-{Guid.NewGuid()}");
            var packedFile = Path.Combine(outDirectory, "capture.fxzy");

            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.SetOutDirectory(outDirectory);

            var scope = new ProxyScope(() => new FluxzyNetOutOfProcessHost(),
                a => new OutOfProcessCaptureContext(a));

            await using (scope) {
                await using var tcpProvider = await CapturedTcpConnectionProvider.Create(scope, true);

                await using var proxy = new Proxy(setting, tcpProvider);

                var endPoint = proxy.Run().First();
                var proxyUrl = $"http://{endPoint.Address}:{endPoint.Port}";

                var (stderr, exitCode) = await RunCurl(proxyUrl, "https://www.fluxzy.io/favicon.ico");

                Assert.True(exitCode == 0, $"curl failed with exit code {exitCode}. stderr: {stderr}");
            }

            using (var directoryReader = new DirectoryArchiveReader(outDirectory)) {
                var exchange = Assert.Single(directoryReader.ReadAllExchanges());

                Assert.Equal(200, exchange.StatusCode);
                Assert.NotEqual(0, exchange.ConnectionId);
            }

            await new FxzyDirectoryPackager().Pack(outDirectory, packedFile);

            using var packedReader = new FluxzyArchiveReader(packedFile);

            var extractedFiles = 0;

            foreach (var connection in packedReader.ReadAllConnections()) {
                await using var captureStream = packedReader.GetRawCaptureStream(connection.Id);

                Assert.NotNull(captureStream);

                var pcapPath = Path.Combine(
                    Path.GetDirectoryName(Path.GetFullPath(packedFile))!,
                    $"{Path.GetFileNameWithoutExtension(packedFile)}-{connection.Id}.pcapng");

                await using (var fileStream = File.Create(pcapPath)) {
                    await captureStream!.CopyToAsync(fileStream);
                }

                Assert.True(new FileInfo(pcapPath).Length > 0, $"{pcapPath} is empty");

                var tcpFlags = await ReadTcpFlags(pcapPath);

                Assert.Contains("S", tcpFlags);
                Assert.Contains("AS", tcpFlags);

                extractedFiles++;
            }

            Assert.True(extractedFiles > 0);
        }

        private static async Task<string[]> ReadTcpFlags(string pcapPath)
        {
            var psi = new ProcessStartInfo {
                FileName = "tshark",
                Arguments = $"-r {pcapPath} -T fields -e tcp.flags.str",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi)!;

            var stdout = await process.StandardOutput.ReadToEndAsync();
            await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            return stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                         .Select(l => new string(l.Where(char.IsLetter).ToArray()))
                         .ToArray();
        }

        private static async Task<(string stderr, int exitCode)> RunCurl(
            string proxyUrl, string url, int timeoutSeconds = 60)
        {
            var psi = new ProcessStartInfo {
                FileName = "curl",
                Arguments = $"-sS -k -o /dev/null --proxy {proxyUrl} {url}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi)!;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

            try {
                var stderr = await process.StandardError.ReadToEndAsync(cts.Token);
                await process.StandardOutput.ReadToEndAsync(cts.Token);
                await process.WaitForExitAsync(cts.Token);

                return (stderr, process.ExitCode);
            }
            catch (OperationCanceledException) {
                try { process.Kill(entireProcessTree: true); } catch { }

                return ($"curl timed out after {timeoutSeconds}s", -1);
            }
        }
    }
}
