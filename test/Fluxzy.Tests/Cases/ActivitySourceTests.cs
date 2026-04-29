// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.Cases
{
    public class ActivitySourceTests
    {
        [Fact]
        public async Task Activity_Is_Emitted_Per_Exchange_With_Otel_Tags()
        {
            var started = new ConcurrentBag<Activity>();
            var stopped = new ConcurrentBag<Activity>();

            using var listener = new ActivityListener {
                ShouldListenTo = source => source.Name == "Fluxzy.Core",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                    ActivitySamplingResult.AllDataAndRecorded,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) =>
                    ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = started.Add,
                ActivityStopped = stopped.Add
            };

            ActivitySource.AddActivityListener(listener);

            await using var setup = await ProxiedHostSetup.Create();

            var response = await setup.Client.GetAsync("/hello");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await response.Content.ReadAsByteArrayAsync();
            response.Dispose();

            // The exchange-completion event drains the response body asynchronously,
            // so give the in-flight activity stop a brief window before asserting.
            for (var i = 0; i < 50 && stopped.IsEmpty; i++)
                await Task.Delay(50);

            Assert.True(started.Any(),
                $"No Fluxzy.Core activity started. ActivitySource.HasListeners()? See: started={started.Count} stopped={stopped.Count}");

            var activity = stopped.FirstOrDefault();
            Assert.NotNull(activity);
            Assert.Equal(ActivityKind.Server, activity.Kind);
            Assert.Equal("HTTP GET", activity.OperationName);

            Assert.Equal("GET", activity.GetTagItem("http.request.method"));
            Assert.NotNull(activity.GetTagItem("url.full"));
            Assert.NotNull(activity.GetTagItem("server.address"));
            Assert.Equal(200, activity.GetTagItem("http.response.status_code"));
            Assert.NotNull(activity.GetTagItem("network.protocol.version"));
            Assert.NotNull(activity.GetTagItem("fluxzy.exchange_id"));
            Assert.NotNull(activity.GetTagItem("fluxzy.pool.type"));
            Assert.NotNull(activity.GetTagItem("fluxzy.dns.duration_ms"));
        }
    }
}
