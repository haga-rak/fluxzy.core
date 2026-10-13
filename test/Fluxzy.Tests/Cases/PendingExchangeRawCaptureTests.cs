// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Archiving;
using Fluxzy.Core;
using Fluxzy.Core.Pcap;
using Fluxzy.Core.Pcap.Cli.Clients;
using Fluxzy.Readers;
using Fluxzy.Rules.Actions;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    /// <summary>
    ///     Raw capture counterpart of <see cref="PendingExchangeArchiveTests"/>: the pcapng
    ///     (and the NSS key log with BouncyCastle) of an unanswered request must survive packing.
    /// </summary>
    [Collection("RUNS_RAW_CAPTURE")]
    public class PendingExchangeRawCaptureTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Unanswered_Request_Keeps_Raw_Capture(bool useBouncyCastle)
        {
            await using var server = TarpitServer.Start(useTls: true);

            var outDirectory = Path.Combine("Drop", $"pending-capture-{Guid.NewGuid()}");

            var setting = FluxzySetting.CreateLocalRandomPort();

            if (useBouncyCastle)
                setting.UseBouncyCastleSslEngine();

            setting.SetOutDirectory(outDirectory);

            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            var scope = new ProxyScope(() => new FluxzyNetOutOfProcessHost(),
                a => new OutOfProcessCaptureContext(a));

            Task<HttpResponseMessage> pendingRequest;

            await using (scope) {
                await using var tcpProvider = await CapturedTcpConnectionProvider.Create(scope, true);

                await using (var proxy = new Proxy(setting, tcpProvider)) {
                    using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);
                    client.Timeout = TimeSpan.FromSeconds(60);

                    pendingRequest = client.GetAsync($"https://local.fluxzy.io:{server.Port}/hang");

                    Assert.True(await server.WaitForRequestCount(1, TimeSpan.FromSeconds(10)));
                }

                try {
                    using var response = await pendingRequest;
                }
                catch {
                    // The proxy went away while the request was in flight
                }
            }

            using var reader = new DirectoryArchiveReader(outDirectory);

            var exchange = Assert.Single(reader.ReadAllExchanges());

            Assert.True(exchange.Pending);
            Assert.NotEqual(0, exchange.ConnectionId);

            var packedFile = Path.Combine(outDirectory, "packed.fxzy");

            await new FxzyDirectoryPackager().Pack(outDirectory, packedFile);

            using var packedReader = new FluxzyArchiveReader(packedFile);

            Assert.NotNull(packedReader.ReadConnection(exchange.ConnectionId));

            using var rawCapture = packedReader.GetRawCaptureStream(exchange.ConnectionId);

            Assert.NotNull(rawCapture);
            Assert.True(rawCapture!.Length > 0);

            if (useBouncyCastle) {
                using var keyLog = packedReader.GetRawCaptureKeyStream(exchange.ConnectionId);

                Assert.NotNull(keyLog);
                Assert.True(keyLog!.Length > 0);
            }
        }
    }
}
