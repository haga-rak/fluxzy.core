// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Core;

namespace Fluxzy.Clients.H2
{
    internal class StreamPool : IDisposable, IAsyncDisposable
    {
        private readonly SemaphoreSlim _maxConcurrentStreamBarrier;
        private readonly ConcurrentDictionary<int, StreamWorker> _runningStreams = new();

        private int _lastStreamIdentifier = -1;
        private bool _onError;

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

        private StreamWorker CreateActiveStream(
            Exchange exchange,
            CancellationToken callerCancellationToken,
            SemaphoreSlim ongoingStreamInit, CancellationTokenSource resetTokenSource)
        {
            if (_onError)
                throw new InvalidOperationException("This connection is on error");

            ongoingStreamInit.Wait(callerCancellationToken);

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
            if (_onError)
                throw new ConnectionCloseException("This connection is on error");

            if (!_maxConcurrentStreamBarrier.Wait(TimeSpan.Zero))
                await _maxConcurrentStreamBarrier.WaitAsync(callerCancellationToken).ConfigureAwait(false);

            var res = CreateActiveStream(exchange, callerCancellationToken, ongoingStreamInit, resetTokenSource);

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

        public void OnGoAway(Exception? ex)
        {
            _onError = ex != null;
            GoAwayException = ex;
        }

        public int ShouldWindowUpdate(int dataLength)
        {
            var windowIncrement = 0;

            _overallWindowSize += dataLength;

            if (_overallWindowSize > 0.5 * Context.Setting.Local.WindowSize) {
                windowIncrement = _overallWindowSize;

                _overallWindowSize = 0;
            }

            return windowIncrement;
        }
    }
}
