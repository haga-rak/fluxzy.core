// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fluxzy.Readers;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters.ResponseFilters;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Rules
{
    public class NoResponseFilterTests
    {
        [Fact]
        public async Task Matches_Archived_Exchange_Without_Response()
        {
            await using var server = TarpitServer.Start(useTls: true);

            var outDirectory = Path.Combine("Drop", $"no-response-filter-{Guid.NewGuid()}");

            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.SetOutDirectory(outDirectory);

            setting.ConfigureRule().WhenAny()
                   .Do(new SkipRemoteCertificateValidationAction());

            Task<HttpResponseMessage> pendingRequest;

            await using (var proxy = new Proxy(setting)) {
                using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);
                client.Timeout = TimeSpan.FromSeconds(60);

                pendingRequest = client.GetAsync($"https://local.fluxzy.io:{server.Port}/hang");

                Assert.True(await server.WaitForRequestCount(1, TimeSpan.FromSeconds(10)));
            }

            try {
                using var response = await pendingRequest;
            }
            catch {
            }

            using var reader = new DirectoryArchiveReader(outDirectory);

            var exchange = Assert.Single(reader.ReadAllExchanges());

            Assert.True(new NoResponseFilter().Apply(null, null!, exchange, null));
            Assert.False(new NoResponseFilter { Inverted = true }.Apply(null, null!, exchange, null));
            Assert.False(new NetworkErrorFilter().Apply(null, null!, exchange, null));
        }

        [Fact]
        public async Task Does_Not_Match_Answered_Or_Failed_Exchanges()
        {
            var outDirectory = Path.Combine("Drop", $"no-response-filter-neg-{Guid.NewGuid()}");

            var setting = FluxzySetting.CreateLocalRandomPort();
            setting.SetOutDirectory(outDirectory);

            await using (var proxy = new Proxy(setting)) {
                using var client = HttpClientUtility.CreateHttpClient(proxy.Run(), setting);

                using var ok = await client.GetAsync($"{TestConstants.PlainHttp11}/global-health-check");
                Assert.True(ok.IsSuccessStatusCode);

                using var refused = await client.GetAsync("http://127.0.0.1:1/");
                Assert.Equal(528, (int) refused.StatusCode);
            }

            using var reader = new DirectoryArchiveReader(outDirectory);

            var exchanges = reader.ReadAllExchanges().ToList();

            Assert.Equal(2, exchanges.Count);
            Assert.All(exchanges, e => Assert.False(new NoResponseFilter().Apply(null, null!, e, null)));
            Assert.All(exchanges, e => Assert.True(new NoResponseFilter { Inverted = true }.Apply(null, null!, e, null)));
        }
    }
}
