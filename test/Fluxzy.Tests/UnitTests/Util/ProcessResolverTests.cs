// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Utils.ProcessTracking;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Util
{
    public class ProcessResolverTests
    {
        private sealed class StubProcessTracker : IProcessTracker
        {
            private readonly ProcessInfo? _result;

            public StubProcessTracker(ProcessInfo? result) => _result = result;

            public int? RequestedPort { get; private set; }

            public ProcessInfo? GetProcessInfo(int localPort)
            {
                RequestedPort = localPort;
                return _result;
            }
        }

        private static ProcessResolutionContext ContextFrom(IPAddress remote, int remotePort)
        {
            return new ProcessResolutionContext(
                new IPEndPoint(remote, remotePort),
                new IPEndPoint(IPAddress.Loopback, 44344),
                new Authority("example.com", 443, true));
        }

        [Fact]
        public async Task Loopback_ReturnsTrackerResult_KeyedOnSourcePort()
        {
            var expected = new ProcessInfo(1234, "/usr/bin/app", "app --flag");
            var tracker = new StubProcessTracker(expected);
            var resolver = new LocalTcpTableProcessResolver(tracker);

            var result = await resolver.ResolveAsync(
                ContextFrom(IPAddress.Loopback, 54321), CancellationToken.None);

            Assert.Same(expected, result);
            Assert.Equal(54321, tracker.RequestedPort);
        }

        [Fact]
        public async Task IPv6Loopback_IsResolved()
        {
            var expected = new ProcessInfo(7, "/bin/x", null);
            var tracker = new StubProcessTracker(expected);
            var resolver = new LocalTcpTableProcessResolver(tracker);

            var result = await resolver.ResolveAsync(
                ContextFrom(IPAddress.IPv6Loopback, 9000), CancellationToken.None);

            Assert.Same(expected, result);
            Assert.Equal(9000, tracker.RequestedPort);
        }

        [Fact]
        public async Task NonLoopback_ReturnsNull_WithoutQueryingTracker()
        {
            var tracker = new StubProcessTracker(new ProcessInfo(1, "x", null));
            var resolver = new LocalTcpTableProcessResolver(tracker);

            var result = await resolver.ResolveAsync(
                ContextFrom(IPAddress.Parse("10.0.0.5"), 54321), CancellationToken.None);

            Assert.Null(result);
            Assert.Null(tracker.RequestedPort);
        }
    }
}
