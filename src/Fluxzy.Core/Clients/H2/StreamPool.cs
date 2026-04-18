// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Core;
using Org.BouncyCastle.Utilities.IO;

namespace Fluxzy.Clients.H2
{
    internal class StreamPool : IDisposable, IAsyncDisposable
    {
        private readonly SemaphoreSlim _maxConcurrentStreamBarrier;
        private readonly ConcurrentDictionary<int, StreamWorker> _runningStreams = new();

        private int _lastStreamIdentifier = -1;
        private volatile bool _draining;

        private int _overallWindowSize;

        public StreamPool(StreamContext context)
        {
            Context = context;
            _maxConcurrentStreamBarrier = new SemaphoreSlim(context.Setting.Remote.SettingsMaxConcurrentStreams);

            _overallWindowSize = context.Setting.Local.WindowSize - context.Setting.Local.MaxFrameSize;
        }

        public StreamContext Context { get; }

        // WARNING : to be improved, may be extreme volatile
        public int ActiveStreamCount => _runningStreams.Count;

        internal Exception? GoAwayException { get; private set; }

        internal int LastStreamIdentifier => _lastStreamIdentifier;

        /// <summary>
        ///     Last stream id the peer reported as processed in a received GOAWAY.
        ///     <c>int.MaxValue</c> until a GOAWAY arrives (meaning "all current streams are
        ///     in range"). After GOAWAY, streams with <c>id &gt; PeerLastStreamId</c> are
        ///     guaranteed by RFC 9113 §6.8 to not have been processed and are safe to retry.
        /// </summary>
        internal int PeerLastStreamId { get; private set; } = int.MaxValue;

        internal bool IsDraining => _draining;

        public ValueTask DisposeAsync()
        {
            return default;
        }

        public void Dispose()
        {
            _maxConcurrentStreamBarrier.Dispose();
        }

        public bool TryGetExistingActiveStream(int streamIdentifier, out StreamWorker? result)
        {
            return _runningStreams.TryGetValue(streamIdentifier, out result);
        }

        private async ValueTask<StreamWorker> CreateActiveStreamAsync(
            Exchange exchange,
            CancellationToken callerCancellationToken,
            SemaphoreSlim ongoingStreamInit, CancellationTokenSource resetTokenSource)
        {
            if (_draining)
                throw new ConnectionCloseException(
                    "This connection is draining after receiving GOAWAY", GoAwayException);

            await ongoingStreamInit.WaitAsync(callerCancellationToken).ConfigureAwait(false);

            var streamId = Interlocked.Add(ref _lastStreamIdentifier, 2);

            var activeStream = new StreamWorker(streamId, this, exchange, resetTokenSource);

            _runningStreams[streamId] = activeStream;

            Context.Logger.Trace(exchange, "Affecting streamIdentifier", streamIdentifier: streamId);

            return activeStream;
        }

        /// <summary>
        ///     Get or create  active stream
        /// </summary>
        /// <returns></returns>
        public async ValueTask<StreamWorker> CreateNewStreamProcessing(
            Exchange exchange,
            CancellationToken callerCancellationToken, SemaphoreSlim ongoingStreamInit,
            CancellationTokenSource resetTokenSource)
        {
            if (_draining)
                throw new ConnectionCloseException(
                    "This connection is draining after receiving GOAWAY", GoAwayException);

            if (!_maxConcurrentStreamBarrier.Wait(TimeSpan.Zero))
                await _maxConcurrentStreamBarrier.WaitAsync(callerCancellationToken).ConfigureAwait(false);

            var res = await CreateActiveStreamAsync(exchange, callerCancellationToken, ongoingStreamInit, resetTokenSource)
                            .ConfigureAwait(false);

            return res;
        }

        public void NotifyDispose(StreamWorker streamWorker)
        {
            // reset can happens here

            if (_runningStreams.TryRemove(streamWorker.StreamIdentifier, out _)) {
                _maxConcurrentStreamBarrier.Release();
                streamWorker.Dispose();
            }
        }

        public void NotifyInitialWindowChange(int newInitialWindow)
        {
            foreach (var kvp in _runningStreams)
            {
                StreamWorker worker = kvp.Value;
                worker.RemoteWindowSize.UpdateInitialWindowSize(newInitialWindow);
            }
        }

        /// <summary>
        ///     Record a received GOAWAY from the remote peer. Always flips the pool to
        ///     draining (no new streams), persists the peer-reported last-processed
        ///     stream id, and proactively abandons any in-flight streams whose id
        ///     exceeds it — per RFC 9113 §6.8 those streams were not processed by the
        ///     server and are safe to retry on a fresh connection.
        /// </summary>
        /// <param name="peerLastStreamId">The <c>LastStreamId</c> field of the GOAWAY.</param>
        /// <param name="errorCode">The error code carried by the GOAWAY.</param>
        /// <param name="cause">
        ///     Non-null for error-coded GOAWAYs. Becomes the inner exception of the
        ///     retryable <see cref="ConnectionCloseException"/> surfaced to callers.
        /// </param>
        public void OnRemoteGoAway(int peerLastStreamId, H2ErrorCode errorCode, Exception? cause)
        {
            PeerLastStreamId = peerLastStreamId;
            GoAwayException = cause;
            _draining = true;

            foreach (var kvp in _runningStreams) {
                if (kvp.Key > peerLastStreamId) {
                    kvp.Value.AbandonAsRetryable(cause);
                }
            }
        }

        public int ShouldWindowUpdate(int dataLength)
        {
            var newValue = Interlocked.Add(ref _overallWindowSize, dataLength);
            var threshold = (int)(0.5 * Context.Setting.Local.WindowSize);

            if (newValue > threshold) {
                // CAS to claim the accumulated value
                var claimed = Interlocked.Exchange(ref _overallWindowSize, 0);

                if (claimed > 0)
                    return claimed;
            }

            return 0;
        }
    }
}
