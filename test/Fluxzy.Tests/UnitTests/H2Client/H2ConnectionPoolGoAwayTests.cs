// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Core;
using Fluxzy.Misc.Streams;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.H2Client
{
    /// <summary>
    ///     Reliability tests for HTTP/2 GOAWAY handling in <see cref="H2ConnectionPool"/>,
    ///     covering the four structural defects that produced the escalating
    ///     <c>OperationCanceledException</c> pattern documented in
    ///     haga-rak/fluxzy.core#634:
    ///
    ///     <list type="number">
    ///         <item>The peer-reported <c>LastStreamId</c> was ignored.</item>
    ///         <item>Graceful GOAWAY never set the pool into draining mode, so new
    ///               exchanges kept allocating doomed stream ids on the pool.</item>
    ///         <item>Non-NoError GOAWAY error codes were silently dropped by
    ///               <c>_goAwayInitByRemote</c>-gated exception suppression.</item>
    ///         <item>Client-emitted GOAWAY frames carried our own outgoing
    ///               <c>LastStreamIdentifier</c> instead of the 0 required by RFC 9113
    ///               §6.8 for a client with push disabled.</item>
    ///     </list>
    /// </summary>
    public class H2ConnectionPoolGoAwayTests
    {
        // ------------------------------------------------------------------
        // Test: graceful GOAWAY flips the pool into draining mode
        // ------------------------------------------------------------------
        [Fact]
        public async Task RemoteGracefulGoAway_PutsPoolInDrainingMode_AndMarksComplete()
        {
            using var pipe = new DuplexPipe();
            var (pool, _) = CreatePool(pipe);
            pool.Init();

            // Server sends GOAWAY(NoError, lastStreamId=7)
            await SendServerGoAway(pipe, lastStreamId: 7, H2ErrorCode.NoError);

            // Wait for the read loop to observe the frame and call OnGoAway.
            await WaitForDrainingAsync(pool);

            var snap = pool.SnapshotForTests();

            Assert.True(snap.Draining, "pool must be draining after graceful GOAWAY");
            Assert.True(pool.Complete, "pool.Complete must be true once draining so PoolBuilder evicts");
            Assert.Equal(7, snap.PeerLastStreamId);
            Assert.Null(snap.GoAwayErrorCode);

            await pool.DisposeAsync();
        }

        // ------------------------------------------------------------------
        // Test: error GOAWAY preserves the error code through the read loop
        // ------------------------------------------------------------------
        [Fact]
        public async Task RemoteErrorGoAway_PreservesErrorCode()
        {
            using var pipe = new DuplexPipe();
            var (pool, _) = CreatePool(pipe);
            pool.Init();

            await SendServerGoAway(pipe, lastStreamId: 0, H2ErrorCode.EnhanceYourCalm);

            await WaitForDrainingAsync(pool);

            var snap = pool.SnapshotForTests();

            Assert.True(snap.Draining);
            Assert.Equal(H2ErrorCode.EnhanceYourCalm, snap.GoAwayErrorCode);
            Assert.NotNull(pool.StreamPoolForTests.GoAwayException);
            Assert.IsType<H2Exception>(pool.StreamPoolForTests.GoAwayException);

            await pool.DisposeAsync();
        }

        // ------------------------------------------------------------------
        // Test: persisted peer LastStreamId
        // ------------------------------------------------------------------
        [Fact]
        public async Task RemoteGoAway_PersistsPeerLastStreamId()
        {
            using var pipe = new DuplexPipe();
            var (pool, _) = CreatePool(pipe);
            pool.Init();

            await SendServerGoAway(pipe, lastStreamId: 42, H2ErrorCode.NoError);

            await WaitForDrainingAsync(pool);

            Assert.Equal(42, pool.SnapshotForTests().PeerLastStreamId);
            Assert.Equal(42, pool.StreamPoolForTests.PeerLastStreamId);

            await pool.DisposeAsync();
        }

        // ------------------------------------------------------------------
        // Test: StreamPool after draining rejects new stream creation
        // ------------------------------------------------------------------
        [Fact]
        public void StreamPool_AfterDraining_RejectsNewStreamCreation()
        {
            var pool = CreateBareStreamPool();

            pool.OnRemoteGoAway(peerLastStreamId: 1, H2ErrorCode.NoError, cause: null);

            var ex = Assert.Throws<ConnectionCloseException>(() =>
                pool.CreateNewStreamProcessing(
                    exchange: MakeExchange(),
                    callerCancellationToken: CancellationToken.None,
                    ongoingStreamInit: new SemaphoreSlim(1, 1),
                    resetTokenSource: new CancellationTokenSource())
                    .AsTask().GetAwaiter().GetResult());

            Assert.Contains("draining", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        // ------------------------------------------------------------------
        // Test: StreamPool proactively abandons streams above peer LastStreamId
        // ------------------------------------------------------------------
        [Fact]
        public async Task StreamPool_OnRemoteGoAway_AbandonsStreamsAboveLastStreamId()
        {
            var pool = CreateBareStreamPool();
            var creationLock = new SemaphoreSlim(1, 1);

            var resetCts1 = new CancellationTokenSource();
            var resetCts3 = new CancellationTokenSource();

            var stream1 = await pool.CreateNewStreamProcessing(
                MakeExchange(), CancellationToken.None, creationLock, resetCts1);

            // In production H2ConnectionPool.InternalSend releases the per-pool creation
            // lock in its finally block; the bare pool fixture has no such caller, so
            // release here to let the second stream's init proceed.
            creationLock.Release();

            var stream3 = await pool.CreateNewStreamProcessing(
                MakeExchange(), CancellationToken.None, creationLock, resetCts3);

            creationLock.Release();

            // Sanity: client-initiated stream ids are odd and increase by 2
            Assert.Equal(1, stream1.StreamIdentifier);
            Assert.Equal(3, stream3.StreamIdentifier);

            pool.OnRemoteGoAway(peerLastStreamId: 1, H2ErrorCode.NoError, cause: null);

            Assert.False(stream1.AbandonedByGoAway, "stream 1 is in range (id <= 1), must NOT be abandoned");
            Assert.True(stream3.AbandonedByGoAway, "stream 3 exceeds lastStreamId, must be proactively abandoned");
            Assert.True(resetCts3.IsCancellationRequested, "abandonment must cancel the stream's reset CTS");
            Assert.False(resetCts1.IsCancellationRequested, "in-range streams must not be cancelled");
        }

        // ------------------------------------------------------------------
        // Test: emitted GOAWAY carries LastStreamId=0 (RFC 9113 §6.8 compliance)
        //
        // Asserts the invariant directly at the frame-build seam rather than through
        // an end-to-end write, because CheckAlive's idle path queues the GOAWAY on
        // the writer channel and then immediately calls OnLoopEnd, which cancels the
        // task before the writer loop drains it — a pre-existing best-effort
        // behaviour. The seam guarantees the LastStreamId never leaks our own outgoing
        // _lastStreamIdentifier (which is -1 before any stream is opened, causing a
        // protocol-invalid negative value on the wire).
        // ------------------------------------------------------------------
        [Theory]
        [InlineData(H2ErrorCode.NoError)]
        [InlineData(H2ErrorCode.EnhanceYourCalm)]
        [InlineData(H2ErrorCode.InternalError)]
        public void EmittedGoAway_CarriesZeroLastStreamId(H2ErrorCode errorCode)
        {
            var bytes = H2ConnectionPool.BuildGoAwayBytes(errorCode);

            // Skip the 9-byte frame header; GoAwayFrame's span constructor reads the body.
            var frame = new GoAwayFrame(bytes.AsSpan(9));

            Assert.Equal(0, frame.LastStreamId);
            Assert.Equal(errorCode, frame.ErrorCode);
        }

        // ------------------------------------------------------------------
        // Test: GOAWAY followed by transport EOF fires fault callback exactly once
        // ------------------------------------------------------------------
        [Fact]
        public async Task OnGoAwayThenEof_DoesNotDoubleInvokeFaultCallback()
        {
            using var pipe = new DuplexPipe();
            var callbackInvocations = 0;

            var (pool, _) = CreatePool(pipe,
                onFault: _ => Interlocked.Increment(ref callbackInvocations));

            pool.Init();

            await SendServerGoAway(pipe, lastStreamId: 0, H2ErrorCode.NoError);
            await WaitForDrainingAsync(pool);

            // Now simulate transport close — the read loop exits and OnLoopEnd fires.
            pipe.ServerWriteStream.Close();

            await pool.DisposeAsync();

            Assert.Equal(1, callbackInvocations);
        }

        // ==================================================================
        // Helpers
        // ==================================================================

        private static (H2ConnectionPool pool, Connection conn) CreatePool(
            DuplexPipe pipe, Action<H2ConnectionPool>? onFault = null)
        {
            var baseStream = new RecomposedStream(pipe.ClientReadStream, pipe.ClientWriteStream);

            var authority = new Authority("test.local", 443, true);
            var setting = new H2StreamSetting();
            var connection = new Connection(authority, new TestIdProvider());

            var pool = new H2ConnectionPool(
                baseStream,
                setting,
                authority,
                connection,
                onConnectionFaulted: onFault ?? (_ => { }));

            return (pool, connection);
        }

        private static async Task SendServerGoAway(
            DuplexPipe pipe, int lastStreamId, H2ErrorCode errorCode)
        {
            var bytes = H2FrameHelper.BuildGoAway(lastStreamId, errorCode);
            await pipe.ServerWriteStream.WriteAsync(bytes);
            await pipe.ServerWriteStream.FlushAsync();
        }

        private static async Task WaitForDrainingAsync(H2ConnectionPool pool)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            while (!pool.SnapshotForTests().Draining) {
                if (sw.Elapsed > TimeSpan.FromSeconds(5))
                    throw new TimeoutException("Pool did not enter draining mode within 5s.");

                await Task.Delay(5);
            }
        }

        // -- Bare StreamPool fixture (no H2ConnectionPool wrapper) ---------

        private static StreamPool CreateBareStreamPool()
        {
            var authority = new Authority("test.local", 443, true);
            var setting = new H2StreamSetting();
            var logger = new H2Logger(authority, -1);

            var hpackEncoder = new HPackEncoder(
                new EncodingContext(ArrayPoolMemoryProvider<char>.Default));
            var hpackDecoder = new HPackDecoder(
                new DecodingContext(authority, ArrayPoolMemoryProvider<char>.Default));
            var headerEncoder = new HeaderEncoder(hpackEncoder, hpackDecoder, setting);

            var overallWindow = new WindowSizeHolder(logger, setting.OverallWindowSize, 0);

            UpStreamChannel noopChannel = (ref WriteTask _) => { };

            var context = new StreamContext(
                connectionId: 1,
                authority: authority,
                setting: setting,
                logger: logger,
                headerEncoder: headerEncoder,
                upStreamChannel: noopChannel,
                overallWindowSizeHolder: overallWindow);

            return new StreamPool(context);
        }

        private static Exchange MakeExchange()
        {
            var authority = new Authority("test.local", 443, true);
            var header = "GET / HTTP/2.0\r\nhost: test.local\r\n\r\n".AsMemory();

            return new Exchange(new TestIdProvider(), authority, header,
                httpVersion: "HTTP/2", DateTime.UtcNow);
        }
    }
}
