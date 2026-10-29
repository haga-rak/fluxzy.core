// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Archiving;
using Fluxzy.Core;
using Fluxzy.Core.Pcap;
using Fluxzy.Core.Pcap.Cli.Clients;
using Fluxzy.Core.Pcap.Pcapng.Structs;
using Fluxzy.Core.Pcap.Reading;
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

            await RunCurlThroughCapturingProxy(outDirectory, 1);

            var pcapFiles = await PackAndExtractCaptures(outDirectory);

            var pcapPath = Assert.Single(pcapFiles);
            var tcpFlags = await ReadTcpFlags(pcapPath);

            Assert.Contains("S", tcpFlags);
            Assert.Contains("AS", tcpFlags);
        }

        [Fact]
        public async Task Sequential_Connections_Each_Keep_Their_Handshake_While_Previous_Writer_Lingers()
        {
            var outDirectory = Path.Combine("Drop", $"curl-favicon-sequential-{Guid.NewGuid()}");
            var originalLinger = FluxzySharedSetting.RawCaptureLingerDelayBeforeTearDownMillis;

            FluxzySharedSetting.RawCaptureLingerDelayBeforeTearDownMillis = 3000;

            var runDirectories = new List<string>();

            try {
                var scope = new ProxyScope(() => new FluxzyNetOutOfProcessHost(),
                    a => new OutOfProcessCaptureContext(a));

                await using (scope) {
                    await using var tcpProvider = await CapturedTcpConnectionProvider.Create(scope, true);

                    for (var i = 0; i < 6; i++) {
                        var runDirectory = Path.Combine(outDirectory, $"run-{i}");
                        runDirectories.Add(runDirectory);

                        await RunCurlThroughProxy(runDirectory, tcpProvider, 1);
                    }
                }
            }
            finally {
                FluxzySharedSetting.RawCaptureLingerDelayBeforeTearDownMillis = originalLinger;
            }

            var pcapFiles = new List<string>();

            foreach (var runDirectory in runDirectories) {
                pcapFiles.AddRange(await PackAndExtractCaptures(runDirectory));
            }

            Assert.Equal(6, pcapFiles.Count);

            foreach (var pcapPath in pcapFiles) {
                var tcpFlags = await ReadTcpFlags(pcapPath);

                Assert.Equal(1, tcpFlags.Count(f => f == "S"));
                Assert.Equal(1, tcpFlags.Count(f => f == "AS"));
                Assert.Equal("S", tcpFlags.First());
            }
        }

        private static async Task RunCurlThroughCapturingProxy(string outDirectory, int requestCount)
        {
            var scope = new ProxyScope(() => new FluxzyNetOutOfProcessHost(),
                a => new OutOfProcessCaptureContext(a));

            await using (scope) {
                await using var tcpProvider = await CapturedTcpConnectionProvider.Create(scope, true);

                await RunCurlThroughProxy(outDirectory, tcpProvider, requestCount);
            }
        }

        private static async Task RunCurlThroughProxy(
            string outDirectory, ITcpConnectionProvider tcpProvider, int requestCount)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.SetOutDirectory(outDirectory);

            await using (var proxy = new Proxy(setting, tcpProvider)) {
                var endPoint = proxy.Run().First();
                var proxyUrl = $"http://{endPoint.Address}:{endPoint.Port}";

                for (var i = 0; i < requestCount; i++) {
                    var (stderr, exitCode) = await RunCurl(proxyUrl, "https://www.fluxzy.io/favicon.ico", outDirectory);

                    Assert.True(exitCode == 0, $"curl failed with exit code {exitCode}. stderr: {stderr}");
                }
            }

            using var directoryReader = new DirectoryArchiveReader(outDirectory);

            var exchanges = directoryReader.ReadAllExchanges().ToList();

            Assert.Equal(requestCount, exchanges.Count);
            Assert.All(exchanges, e => Assert.Equal(200, e.StatusCode));
        }

        private static async Task<List<string>> PackAndExtractCaptures(string outDirectory)
        {
            var packedFile = Path.Combine(outDirectory, "capture.fxzy");

            await new FxzyDirectoryPackager().Pack(outDirectory, packedFile);

            using var packedReader = new FluxzyArchiveReader(packedFile);

            var extracted = new List<string>();

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

                extracted.Add(pcapPath);
            }

            return extracted;
        }

        private static Task<string[]> ReadTcpFlags(string pcapPath)
        {
            var bytes = File.ReadAllBytes(pcapPath);
            var flags = new List<string>();
            var offset = 0;

            while (offset + 8 <= bytes.Length) {
                var blockType = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));
                var blockLength = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset + 4));

                if (blockType == EnhancedPacketBlock.BlockTypeValue) {
                    var capturedLength = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset + 20));
                    var packet = bytes.AsSpan(offset + 28, capturedLength);

                    var ethernet = new EthernetPacketInfo();

                    if (RawPacketParser.TryParseEthernet(ref ethernet, packet, out var ethernetLength)) {
                        var ip = new IpPacketInfo();
                        var ipSlice = packet.Slice(ethernetLength);

                        if (RawPacketParser.TryParseIp(ref ip, ipSlice) && ip.IsTcp) {
                            var tcp = ipSlice.Slice(ip.HeaderLength);
                            flags.Add(FormatTcpFlags(tcp[13]));
                        }
                    }
                }

                offset += blockLength;
            }

            return Task.FromResult(flags.ToArray());
        }

        private static string FormatTcpFlags(byte flags)
        {
            var result = string.Empty;

            if ((flags & 0x10) != 0)
                result += "A";

            if ((flags & 0x08) != 0)
                result += "P";

            if ((flags & 0x04) != 0)
                result += "R";

            if ((flags & 0x02) != 0)
                result += "S";

            if ((flags & 0x01) != 0)
                result += "F";

            return result;
        }

        private static async Task<(string stderr, int exitCode)> RunCurl(
            string proxyUrl, string url, string outDirectory, int timeoutSeconds = 60)
        {
            Directory.CreateDirectory(outDirectory);

            var bodyFile = Path.GetFullPath(Path.Combine(outDirectory, $"curl-{Guid.NewGuid()}.body"));

            var psi = new ProcessStartInfo {
                FileName = "curl",
                Arguments = $"-sS -k -o \"{bodyFile}\" --proxy {proxyUrl} {url}",
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
            finally {
                try { File.Delete(bodyFile); } catch { }
            }
        }
    }
}
