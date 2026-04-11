// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.H2;
using Fluxzy.Core;
using Fluxzy.Misc.Streams;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.H2Client
{
    /// <summary>
    ///     Reproducers for the H2 connection-pool disposal race documented in
    ///     haga-rak/fluxzy.core#614.
    ///
    ///     These tests are RED on the current code and document the structural
    ///     defects that need to be fixed:
    ///
    ///     1) <see cref="CheckAlive_IdleTeardown_DrainsWriterChannelAndCancelsCtsBeforeFaultCallback"/>
    ///        — <c>H2ConnectionPool.OnLoopEnd</c> calls the fault callback BEFORE it
    ///        cancels its CTS and drains its writer channel, so the callback (which in
    ///        production fire-and-forgets <c>DisposeAsync</c>) races with the rest of
    ///        OnLoopEnd's cleanup.
    ///
    ///     2) <see cref="CheckAllPoolsOnceAsync_WhenPoolThrows_DoesNotPropagate"/>
    ///        — <c>PoolBuilder.CheckPoolStatus</c> only catches
    ///        <see cref="TaskCanceledException"/>, so any other exception from a pool's
    ///        <c>CheckAlive</c> escapes the async-void method and crashes the process.
    /// </summary>
    public class H2ConnectionPoolDisposalTests
    {
        // ------------------------------------------------------------------
        // Test 1: structural ordering invariant
        // ------------------------------------------------------------------
        [Fact]
        public async Task CheckAlive_IdleTeardown_DrainsWriterChannelAndCancelsCtsBeforeFaultCallback()
        {
            // Arrange ----------------------------------------------------------
            using var pipe = new DuplexPipe();

            // The pool's base stream is a single bidirectional stream, recomposed
            // from the two unidirectional pipe halves on the client side.
            var baseStream = new RecomposedStream(pipe.ClientReadStream, pipe.ClientWriteStream);

            var authority = new Authority("test.local", 443, true);
            var setting = new H2StreamSetting {
                // 0 means "any positive elapsed time qualifies as idle"; combined with
                // setting LastActivity to MinValue below, the very first CheckAlive
                // call deterministically takes the idle-teardown branch.
                MaxIdleSeconds = 0
            };
            var connection = new Connection(authority, new TestIdProvider());

            // Snapshot of the pool's internal state at the moment the fault callback
            // fires. Captured non-destructively via SnapshotForTests so the assertions
            // can run after the pool has been quiesced.
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

            // Force the idle branch deterministically (no wall-clock dependency).
            pool.LastActivity = DateTime.MinValue;

            // Act --------------------------------------------------------------
            // CheckAlive observes idle, calls EmitGoAway (queues a GoAway WriteTask
            // on the writer channel), then calls OnLoopEnd(null, true). OnLoopEnd
            // is the unit under test for the ordering invariant.
            await pool.CheckAlive();

            // Quiesce: dispose the pool so the read/write loops exit cleanly before
            // we tear down the duplex pipe in the using-block.
            await pool.DisposeAsync();

            // Assert -----------------------------------------------------------
            Assert.Equal(1, faultCallbackInvocations);
            Assert.NotNull(snapshotAtCallback);

            var snap = snapshotAtCallback!.Value;

            // The fault callback should observe a pool that has already finished its
            // own internal cleanup. Today this is NOT the case — OnLoopEnd notifies
            // the callback first and only then cancels the CTS / drains the channel.

            Assert.True(
                snap.Complete,
                "_complete must be set before the fault callback runs (currently true)");

            Assert.True(
                snap.CtsCancelled,
                "the connection CTS must be cancelled before the fault callback runs " +
                "(currently NOT — OnLoopEnd cancels after returning from the callback)");

            Assert.True(
                snap.WriterChannelDrainedAndClosed,
                "the writer channel must be completed and drained before the fault " +
                "callback runs (currently NOT — OnLoopEnd drains after the callback)");

            Assert.Equal(0, snap.WriterChannelPendingCount);
        }

        // ------------------------------------------------------------------
        // Test 4: async-void crash vector
        // ------------------------------------------------------------------
        [Fact]
        public async Task CheckAllPoolsOnceAsync_WhenPoolThrows_DoesNotPropagate()
        {
            // Arrange ----------------------------------------------------------
            // We only exercise the per-tick check pass via the internal seam; the
            // GetPool collaborator stack is never touched, so we can pass null! for
            // the four constructor dependencies.
            var poolBuilder = new PoolBuilder(
                remoteConnectionBuilder: null!,
                timingProvider: null!,
                archiveWriter: null!,
                dnsSolver: null!);

            // Cancel the background CheckPoolStatus loop immediately so it can't
            // race with our test by hitting the throwing pool on its own 5s tick.
            poolBuilder.Dispose();

            var authority = new Authority("throwing.local", 443, true);
            var throwingPool = new ThrowingHttpConnectionPool(authority);
            poolBuilder.TryAddPoolForTests(authority, throwingPool);

            // Act --------------------------------------------------------------
            Exception? caught = null;
            try {
                await poolBuilder.CheckAllPoolsOnceAsync();
            }
            catch (Exception ex) {
                caught = ex;
            }

            // Assert -----------------------------------------------------------
            // In production, this exception would escape the async-void
            // CheckPoolStatus and terminate the process. The fix must catch and log
            // it inside the check loop instead of propagating.
            Assert.Null(caught);
            Assert.Equal(1, throwingPool.CheckAliveInvocations);
        }

        // ------------------------------------------------------------------
        // Test doubles
        // ------------------------------------------------------------------

        /// <summary>
        ///     Stub <see cref="IHttpConnectionPool"/> whose <see cref="CheckAlive"/>
        ///     throws a <see cref="NullReferenceException"/> — mirroring the prod
        ///     symptom in haga-rak/fluxzy.core#614. Other members are unused by the
        ///     test and throw if invoked.
        /// </summary>
        private sealed class ThrowingHttpConnectionPool : IHttpConnectionPool
        {
            public ThrowingHttpConnectionPool(Authority authority)
            {
                Authority = authority;
            }

            public Authority Authority { get; }

            public bool Complete => false;

            public int CheckAliveInvocations { get; private set; }

            public void Init()
            {
            }

            public ValueTask<bool> CheckAlive()
            {
                CheckAliveInvocations++;
                throw new NullReferenceException(
                    "Simulated NRE from H2ConnectionPool.OnLoopEnd (haga-rak/fluxzy.core#614)");
            }

            public ValueTask Send(
                Exchange exchange,
                IDownStreamPipe downstreamPipe,
                Fluxzy.Misc.ResizableBuffers.RsBuffer buffer,
                ExchangeScope exchangeScope,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException("Send is not exercised by this test.");
            }

            public ValueTask DisposeAsync()
            {
                return default;
            }
        }
    }
}
