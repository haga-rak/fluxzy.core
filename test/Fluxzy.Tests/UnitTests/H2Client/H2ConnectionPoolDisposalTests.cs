// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H2;
using Fluxzy.Core;
using Fluxzy.Misc.Streams;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.H2Client
{
    /// <summary>
    ///     Reproducer for the H2 connection-pool disposal race documented in
    ///     haga-rak/fluxzy.core#614.
    ///
    ///     Idle teardown now lives in <see cref="H2ConnectionPool.TryIdleTeardown"/>,
    ///     driven by a per-connection <see cref="System.Threading.PeriodicTimer"/>
    ///     started in <c>Init</c>. The test calls the seam directly so the assertion
    ///     does not depend on wall-clock timer firing.
    /// </summary>
    public class H2ConnectionPoolDisposalTests
    {
        [Fact]
        public async Task TryIdleTeardown_DrainsWriterChannelAndCancelsCtsBeforeFaultCallback()
        {
            using var pipe = new DuplexPipe();

            var baseStream = new RecomposedStream(pipe.ClientReadStream, pipe.ClientWriteStream);

            var authority = new Authority("test.local", 443, true);
            var setting = new H2StreamSetting {
                // 0 means "any positive elapsed time qualifies as idle"; combined with
                // setting LastActivity to MinValue below, the very first TryIdleTeardown
                // call deterministically takes the idle-teardown branch.
                MaxIdleSeconds = 0
            };
            var connection = new Connection(authority, new TestIdProvider());

            H2ConnectionPool.H2ConnectionPoolStateSnapshot? snapshotAtCallback = null;
            var faultCallbackInvocations = 0;

            var pool = new H2ConnectionPool(
                baseStream,
                setting,
                authority,
                connection,
                onConnectionFaulted: p => {
                    Interlocked.Increment(ref faultCallbackInvocations);
                    snapshotAtCallback = p.SnapshotForTests();
                });

            pool.Init();

            pool.LastActivity = DateTime.MinValue;

            // Drive idle teardown deterministically (no wall-clock dependency).
            var tornDown = pool.TryIdleTeardown();

            await pool.DisposeAsync();

            Assert.True(tornDown, "TryIdleTeardown must report it took the teardown branch");
            Assert.Equal(1, faultCallbackInvocations);
            Assert.NotNull(snapshotAtCallback);

            var snap = snapshotAtCallback!.Value;

            // The fault callback should observe a pool that has already finished its
            // own internal cleanup. OnLoopEnd cancels the CTS / drains the writer
            // channel BEFORE notifying the callback.
            Assert.True(snap.Complete,
                "_complete must be set before the fault callback runs");

            Assert.True(snap.CtsCancelled,
                "the connection CTS must be cancelled before the fault callback runs");

            Assert.True(snap.WriterChannelDrainedAndClosed,
                "the writer channel must be completed and drained before the fault callback runs");

            Assert.Equal(0, snap.WriterChannelPendingCount);
        }
    }
}
