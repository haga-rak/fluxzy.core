// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Core;
using Xunit;

namespace Fluxzy.Tests.UnitTests.H2Client
{
    /// <summary>
    ///     Reproducer for haga-rak/fluxzy.core#634 — "H2ConnectionPool: stray
    ///     OperationCanceledException escalating over time".
    ///
    ///     <para>
    ///         Every HTTP/2 stream creation acquires one permit from
    ///         <c>StreamPool._maxConcurrentStreamBarrier</c> (capacity =
    ///         <see cref="PeerSetting.SettingsMaxConcurrentStreams"/>). That permit is
    ///         released in exactly one place — <see cref="StreamPool.NotifyDispose"/>,
    ///         which only runs once the <see cref="StreamWorker"/> has been registered in
    ///         <c>_runningStreams</c> and later torn down.
    ///     </para>
    ///
    ///     <para>
    ///         If the caller's (or the linked pool/stream) token is cancelled <em>during
    ///         stream setup</em> — after the permit is taken in
    ///         <see cref="StreamPool.CreateNewStreamProcessing"/> but before the worker is
    ///         registered in <c>CreateActiveStreamAsync</c> — the method throws
    ///         <see cref="OperationCanceledException"/> and the permit is never returned.
    ///         Under load this happens constantly (HttpClient timeouts, client aborts).
    ///         On a long-lived, reused pool the permits drain monotonically to zero; once
    ///         exhausted, every subsequent stream blocks on the barrier until the caller's
    ///         own timeout fires — surfacing as the flood of stray
    ///         <see cref="OperationCanceledException"/> the issue reports, escalating over
    ///         time, with throughput collapsing. Reverting to HTTP/1.1 (no per-stream
    ///         barrier) sidesteps it entirely, matching the reporter's observation.
    ///     </para>
    ///
    ///     The test drives the cancellation deterministically (pre-cancelled token, no
    ///     wall-clock waits) and asserts the invariant: with zero active streams, the pool
    ///     must hold all of its concurrency permits.
    /// </summary>
    public class H2StreamPoolConcurrencyLeakTests
    {
        [Fact]
        public async Task StreamSetupCancellation_DoesNotLeakConcurrencyPermits()
        {
            const int maxConcurrent = 2;

            var setting = new H2StreamSetting();
            setting.Remote.SettingsMaxConcurrentStreams = maxConcurrent;

            var pool = BuildStreamPool(setting);

            Assert.Equal(maxConcurrent, AvailablePermits(pool));

            // Simulate caller/stream cancellations that land *during stream setup*: the
            // barrier permit is taken, then the wait on the stream-creation lock throws
            // OperationCanceledException before the StreamWorker is registered.
            for (var i = 0; i < maxConcurrent; i++) {
                using var cancelled = new CancellationTokenSource();
                cancelled.Cancel();

                // An available lock — the cancellation, not contention, is what aborts the
                // setup. WaitAsync on an already-cancelled token throws without consuming
                // this permit, so the *only* permit in flight is the barrier one.
                var ongoingInit = new SemaphoreSlim(1, 1);
                using var resetCts = new CancellationTokenSource();

                await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    pool.CreateNewStreamProcessing(null!, cancelled.Token, ongoingInit, resetCts).AsTask());
            }

            // No stream ever became active — every attempt aborted before registration.
            Assert.Equal(0, pool.ActiveStreamCount);

            // INVARIANT (fails on buggy code): zero active streams => all permits available.
            var leaked = maxConcurrent - AvailablePermits(pool);
            Assert.True(leaked == 0,
                $"Leaked {leaked} of {maxConcurrent} concurrency permits while ActiveStreamCount=0. " +
                $"On a long-lived H2 pool these drain to zero and every new stream stalls, " +
                $"producing the escalating OperationCanceledException of issue #634.");

            // BEHAVIOURAL PROOF: with the permits returned, a clean uncontended creation
            // succeeds promptly. On buggy code the barrier is depleted and this blocks
            // until the 5s timeout, throwing the stray OperationCanceledException directly.
            using var liveReset = new CancellationTokenSource();
            var liveInit = new SemaphoreSlim(1, 1);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var stream = await pool.CreateNewStreamProcessing(null!, timeout.Token, liveInit, liveReset);
            Assert.NotNull(stream);
        }

        private static StreamPool BuildStreamPool(H2StreamSetting setting)
        {
            var authority = new Authority("test.local", 443, true);

            var hPackEncoder = new HPackEncoder(new EncodingContext(ArrayPoolMemoryProvider<char>.Default));
            var hPackDecoder = new HPackDecoder(new DecodingContext(authority, ArrayPoolMemoryProvider<char>.Default));
            var headerEncoder = new HeaderEncoder(hPackEncoder, hPackDecoder, setting);

            var overallWindow = new WindowSizeHolder(setting.OverallWindowSize, 0);

            UpStreamChannel upStreamChannel = (ref WriteTask _) => { };

            var context = new StreamContext(
                connectionId: 1,
                authority,
                setting,
                headerEncoder,
                upStreamChannel,
                overallWindow);

            return new StreamPool(context);
        }

        private static int AvailablePermits(StreamPool pool)
        {
            var field = typeof(StreamPool).GetField(
                "_maxConcurrentStreamBarrier",
                BindingFlags.Instance | BindingFlags.NonPublic);

            var semaphore = (SemaphoreSlim) field!.GetValue(pool)!;
            return semaphore.CurrentCount;
        }
    }
}
