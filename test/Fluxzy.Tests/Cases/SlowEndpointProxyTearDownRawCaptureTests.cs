// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
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
using Fluxzy.Readers;
using Fluxzy.Rules.Actions;
using Fluxzy.Tests._Fixtures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    [Collection("RUNS_RAW_CAPTURE")]
    public class SlowEndpointProxyTearDownRawCaptureTests
    {
        [Fact]
        public async Task Proxy_Torn_Down_Before_Slow_Endpoint_Answers_Keeps_Connection_And_Pcap()
        {
            var requestReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await using var host = await InProcessHost.Create(app => {
                app.MapGet("/slow", async (HttpContext ctx) => {
                    requestReceived.TrySetResult();
                    await Task.Delay(TimeSpan.FromSeconds(60), ctx.RequestAborted);

                    return Results.Ok("late");
                });
            }, suppressLogging: true);

            var outDirectory = Path.Combine("Drop", $"slow-endpoint-capture-{Guid.NewGuid()}");
            Directory.CreateDirectory(outDirectory);

            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.SetOutDirectory(outDirectory);

            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            var scope = new ProxyScope(() => new FluxzyNetOutOfProcessHost(),
                a => new OutOfProcessCaptureContext(a));

            Process? curl = null;

            try {
                await using (scope) {
                    await using var tcpProvider = await CapturedTcpConnectionProvider.Create(scope, true);

                    await using (var proxy = new Proxy(setting, tcpProvider)) {
                        var endPoint = proxy.Run().First();
                        var proxyUrl = $"http://{endPoint.Address}:{endPoint.Port}";

                        var started = Stopwatch.StartNew();

                        curl = StartCurl(proxyUrl, $"{host.BaseUrl}/slow");

                        var arrived = await Task.WhenAny(requestReceived.Task, Task.Delay(TimeSpan.FromSeconds(10)));

                        Assert.True(arrived == requestReceived.Task, "Kestrel never received the proxied request");

                        var remaining = TimeSpan.FromSeconds(3) - started.Elapsed;

                        if (remaining > TimeSpan.Zero)
                            await Task.Delay(remaining);
                    }
                }

                using var exited = new CancellationTokenSource(TimeSpan.FromSeconds(15));

                try {
                    await curl.WaitForExitAsync(exited.Token);
                }
                catch (OperationCanceledException) {
                }

                Assert.True(curl.HasExited, "curl should have been released by the proxy tear down");
                Assert.NotEqual(0, curl.ExitCode);
            }
            finally {
                if (curl != null) {
                    try { curl.Kill(entireProcessTree: true); } catch { }
                    curl.Dispose();
                }
            }

            using var reader = new DirectoryArchiveReader(outDirectory);

            var exchange = Assert.Single(reader.ReadAllExchanges());

            Assert.True(exchange.Pending);
            Assert.NotEqual(0, exchange.ConnectionId);

            var connection = reader.ReadConnection(exchange.ConnectionId);

            Assert.NotNull(connection);
            Assert.Equal(host.Port, connection!.Authority.Port);

            var packedFile = Path.Combine(outDirectory, "packed.fxzy");

            await new FxzyDirectoryPackager().Pack(outDirectory, packedFile);

            using var packedReader = new FluxzyArchiveReader(packedFile);

            Assert.NotNull(packedReader.ReadConnection(exchange.ConnectionId));
            var pcapBytes = await ReadAll(packedReader.GetRawCaptureStream(exchange.ConnectionId));

            Assert.True(pcapBytes.Length > 0);
            Assert.True(CountPacketBlocks(pcapBytes) > 0, "pcapng holds no packet block, only the section header");
        }

        private static int CountPacketBlocks(byte[] bytes)
        {
            var count = 0;
            var offset = 0;

            while (offset + 8 <= bytes.Length) {
                var blockType = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));
                var blockLength = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset + 4));

                if (blockType == EnhancedPacketBlock.BlockTypeValue)
                    count++;

                offset += blockLength;
            }

            return count;
        }

        private static Process StartCurl(string proxyUrl, string url)
        {
            var psi = new ProcessStartInfo {
                FileName = "curl",
                Arguments = $"-sS -k -o /dev/null --proxy {proxyUrl} {url}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(psi)!;

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return process;
        }

        private static async Task<byte[]> ReadAll(Stream? stream)
        {
            Assert.NotNull(stream);

            using var _ = stream;
            using var sink = new MemoryStream();

            await stream!.CopyToAsync(sink);

            return sink.ToArray();
        }
    }
}
