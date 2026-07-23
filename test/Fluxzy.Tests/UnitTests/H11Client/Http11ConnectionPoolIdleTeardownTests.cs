// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using Fluxzy.Clients;
using Fluxzy.Clients.H11;
using Fluxzy.Core;
using Xunit;

namespace Fluxzy.Tests.UnitTests.H11Client
{
    /// <summary>
    ///     Reproducer for the HTTP/1.1 pool accumulation confirmed in the heap dump for
    ///     haga-rak/fluxzy.core#634: an idle <see cref="Http11ConnectionPool"/> was never
    ///     evicted from PoolBuilder (<c>Complete</c> hardcoded false, H2-only fault callback,
    ///     no-op dispose), so a proxy hitting many hosts accumulated one immortal pool per
    ///     host. Drives the new <see cref="Http11ConnectionPool.TryIdleTeardown"/> seam
    ///     directly (no wall-clock waits).
    /// </summary>
    public class Http11ConnectionPoolIdleTeardownTests
    {
        [Fact]
        public void IdlePool_TearsDown_DisposesPooledConnections_AndFiresEviction()
        {
            IHttpConnectionPool? evicted = null;
            var pool = BuildPool(onFaulted: p => evicted = p);

            // An idle recycled connection — what pins a socket + TLS state in the dump.
            var (connection, readStream, writeStream) = MakePooledConnection();
            Assert.True(EnqueuePooledConnection(pool, connection),
                "precondition: pooled connection enqueued");

            pool.LastActivity = DateTime.MinValue; // force the idle clock

            var tornDown = pool.TryIdleTeardown();

            Assert.True(tornDown, "an idle pool must report that it tore down");
            Assert.True(pool.Complete,
                "a torn-down pool must be Complete so PoolBuilder evicts it and stops reusing it");
            Assert.Same(pool, evicted); // eviction callback fired with this pool

            // The pooled connection's transport must be released (the missing reclamation).
            Assert.True(readStream.Disposed, "pooled connection read stream must be disposed");
            Assert.True(writeStream.Disposed, "pooled connection write stream must be disposed");
        }

        [Fact]
        public void RecentlyUsedPool_IsNotTornDown()
        {
            var faulted = 0;
            var pool = BuildPool(onFaulted: _ => Interlocked.Increment(ref faulted));

            pool.LastActivity = DateTime.UtcNow; // just used

            Assert.False(pool.TryIdleTeardown(), "a recently-used pool must not be torn down");
            Assert.False(pool.Complete);
            Assert.Equal(0, faulted);
        }

        [Fact]
        public void PoolWithInflightRequest_IsNotTornDown()
        {
            var faulted = 0;
            var pool = BuildPool(onFaulted: _ => Interlocked.Increment(ref faulted));

            SetActiveRequestCount(pool, 1);        // simulate an in-flight Send
            pool.LastActivity = DateTime.MinValue; // and idle by the wall clock

            Assert.False(pool.TryIdleTeardown(),
                "a pool with an in-flight request must not be evicted from under it");
            Assert.False(pool.Complete);
            Assert.Equal(0, faulted);
        }

        /// <summary>
        ///     Reproducer for the socket/handle accumulation reported in
        ///     haga-rak/fluxzy.core#744: an expired keep-alive connection popped during
        ///     <c>Send</c> was dropped without disposal, leaking its transport.
        /// </summary>
        [Fact]
        public void ExpiredPooledConnection_IsReleased_OnDequeue()
        {
            var pool = BuildPool(onFaulted: _ => { });

            var (connection, readStream, writeStream) = MakePooledConnection();
            Assert.True(EnqueuePooledConnection(pool, connection, DateTime.UtcNow.AddMinutes(-5)),
                "precondition: expired pooled connection enqueued");

            var dequeued = pool.DequeueReusableConnection(DateTime.UtcNow);

            Assert.Null(dequeued);
            Assert.True(readStream.Disposed, "expired connection read stream must be disposed");
            Assert.True(writeStream.Disposed, "expired connection write stream must be disposed");
        }

        [Fact]
        public void Dequeue_ReleasesExpired_AndReturnsFreshConnection()
        {
            var pool = BuildPool(onFaulted: _ => { });

            var (expired, expiredRead, expiredWrite) = MakePooledConnection();
            var (fresh, freshRead, freshWrite) = MakePooledConnection();

            Assert.True(EnqueuePooledConnection(pool, expired, DateTime.UtcNow.AddMinutes(-5)));
            Assert.True(EnqueuePooledConnection(pool, fresh, DateTime.UtcNow));

            var dequeued = pool.DequeueReusableConnection(DateTime.UtcNow);

            Assert.Same(fresh, dequeued);
            Assert.True(expiredRead.Disposed, "expired connection read stream must be disposed");
            Assert.True(expiredWrite.Disposed, "expired connection write stream must be disposed");
            Assert.False(freshRead.Disposed, "reusable connection must not be disposed");
            Assert.False(freshWrite.Disposed, "reusable connection must not be disposed");
        }

        // ==================================================================
        // Helpers
        // ==================================================================

        private static Http11ConnectionPool BuildPool(Action<IHttpConnectionPool> onFaulted)
        {
            var authority = new Authority("test.local", 443, true);

            // remoteConnectionBuilder/archiveWriter/DNS are only used by Send, not teardown.
            // Init() is skipped on purpose so no background timer is left running.
            return new Http11ConnectionPool(
                authority,
                remoteConnectionBuilder: null!,
                ITimingProvider.Default,
                ProxyRuntimeSetting.CreateDefault,
                archiveWriter: null!,
                resolutionResult: default,
                onConnectionFaulted: onFaulted);
        }

        private static (Connection connection, RecordingStream read, RecordingStream write) MakePooledConnection()
        {
            var connection = new Connection(new Authority("test.local", 443, true), IIdProvider.FromZero);
            var read = new RecordingStream();
            var write = new RecordingStream();
            connection.ReadStream = read;
            connection.WriteStream = write;
            return (connection, read, write);
        }

        private static bool EnqueuePooledConnection(
            Http11ConnectionPool pool, Connection connection, DateTime? lastUsed = null)
        {
            var field = typeof(Http11ConnectionPool).GetField(
                "_pendingConnections", BindingFlags.Instance | BindingFlags.NonPublic);

            var channel = (Channel<Http11ProcessingState>) field!.GetValue(pool)!;
            return channel.Writer.TryWrite(new Http11ProcessingState(connection, lastUsed ?? DateTime.UtcNow));
        }

        private static void SetActiveRequestCount(Http11ConnectionPool pool, int count)
        {
            typeof(Http11ConnectionPool)
                .GetField("_activeRequestCount", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(pool, count);
        }

        private sealed class RecordingStream : Stream
        {
            public bool Disposed { get; private set; }

            protected override void Dispose(bool disposing)
            {
                Disposed = true;
                base.Dispose(disposing);
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => 0;
            public override long Position { get => 0; set { } }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => 0;
            public override long Seek(long offset, SeekOrigin origin) => 0;
            public override void SetLength(long value) { }
            public override void Write(byte[] buffer, int offset, int count) { }
        }
    }
}
