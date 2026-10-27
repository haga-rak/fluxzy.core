// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Archiving;
using Fluxzy.Readers;
using Fluxzy.Rules.Actions;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    /// <summary>
    ///     An exchange whose request was sent but never answered before the proxy stops
    ///     must keep its connection in the archive (haga-rak/fluxzy#484).
    /// </summary>
    public class PendingExchangeArchiveTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task New_Connection_Unanswered_Request_Keeps_Connection(bool useBouncyCastle)
        {
            await using var server = TarpitServer.Start(useTls: true);

            var outDirectory = Path.Combine("Drop", $"pending-new-{Guid.NewGuid()}");
            var setting = CreateSetting(useBouncyCastle, outDirectory);

            Task<HttpResponseMessage> pendingRequest;

            await using (var proxy = new Proxy(setting)) {
                using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);
                client.Timeout = TimeSpan.FromSeconds(60);

                pendingRequest = client.GetAsync($"https://local.fluxzy.io:{server.Port}/hang");

                Assert.True(await server.WaitForRequestCount(1, TimeSpan.FromSeconds(10)));
            }

            await ObserveAbort(pendingRequest);

            using var reader = new DirectoryArchiveReader(outDirectory);

            var exchange = Assert.Single(reader.ReadAllExchanges());

            Assert.True(exchange.Pending);
            Assert.NotEqual(0, exchange.ConnectionId);

            var connection = reader.ReadConnection(exchange.ConnectionId);

            Assert.NotNull(connection);
            Assert.NotEqual(default, connection!.TcpConnectionOpened);
            Assert.NotEqual(default, connection.SslNegotiationEnd);

            await AssertPackedArchiveKeepsConnection(outDirectory, exchange.ConnectionId);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Pooled_Connection_Unanswered_Second_Request_Keeps_Connection(bool useBouncyCastle)
        {
            await using var server = TarpitServer.Start(useTls: true, answerFirstRequest: true);

            var outDirectory = Path.Combine("Drop", $"pending-pooled-{Guid.NewGuid()}");
            var setting = CreateSetting(useBouncyCastle, outDirectory);

            Task<HttpResponseMessage> pendingRequest;

            await using (var proxy = new Proxy(setting)) {
                using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);
                client.Timeout = TimeSpan.FromSeconds(60);

                var baseUrl = $"https://local.fluxzy.io:{server.Port}";

                using var first = await client.GetAsync($"{baseUrl}/first");

                Assert.Equal("ok", await first.Content.ReadAsStringAsync());

                await Task.Delay(200);

                pendingRequest = client.GetAsync($"{baseUrl}/hang");

                Assert.True(await server.WaitForRequestCount(2, TimeSpan.FromSeconds(10)));
            }

            await ObserveAbort(pendingRequest);

            using var reader = new DirectoryArchiveReader(outDirectory);

            var exchanges = reader.ReadAllExchanges().OrderBy(e => e.Id).ToList();

            Assert.Equal(2, exchanges.Count);

            var completed = exchanges[0];
            var pending = exchanges[1];

            Assert.False(completed.Pending);
            Assert.True(pending.Pending);
            Assert.NotEqual(0, completed.ConnectionId);
            Assert.Equal(completed.ConnectionId, pending.ConnectionId);

            await AssertPackedArchiveKeepsConnection(outDirectory, pending.ConnectionId);
        }

        private static FluxzySetting CreateSetting(bool useBouncyCastle, string outDirectory)
        {
            var setting = FluxzySetting.CreateLocalRandomPort();

            if (useBouncyCastle)
                setting.UseBouncyCastleSslEngine();

            setting.SetOutDirectory(outDirectory);

            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            return setting;
        }

        private static async Task ObserveAbort(Task<HttpResponseMessage> pendingRequest)
        {
            try {
                using var response = await pendingRequest;
            }
            catch {
                // The proxy went away while the request was in flight
            }
        }

        private static async Task AssertPackedArchiveKeepsConnection(string outDirectory, int connectionId)
        {
            var packedFile = Path.Combine(outDirectory, "packed.fxzy");

            await new FxzyDirectoryPackager().Pack(outDirectory, packedFile);

            using var packedReader = new FluxzyArchiveReader(packedFile);

            Assert.NotNull(packedReader.ReadConnection(connectionId));
            Assert.Contains(packedReader.ReadAllExchanges(), e => e.ConnectionId == connectionId && e.Pending);
        }
    }
}
