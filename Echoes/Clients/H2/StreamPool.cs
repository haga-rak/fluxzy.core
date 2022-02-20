using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Helpers;

namespace Echoes.H2
{
    internal class StreamPool :  IDisposable, IAsyncDisposable
    {
        private readonly IDictionary<int, StreamManager> _runningStreams = new Dictionary<int, StreamManager>();

        private int _nextStreamIdentifier = -1;

        private readonly SemaphoreSlim _maxConcurrentStreamBarrier;
        private bool _onError;

        private readonly FifoLock _fifoLock = new();
        private int _overallWindowSize;

        public StreamPool(
            StreamContext context)
        {
            Context = context;
            _maxConcurrentStreamBarrier = new SemaphoreSlim((int) context.Setting.Remote.SettingsMaxConcurrentStreams);

            _overallWindowSize  = context.Setting.Local.WindowSize - context.Setting.Local.MaxFrameSize;
        }
        
        public StreamContext Context { get; }

        public bool TryGetExistingActiveStream(int streamIdentifier, out StreamManager result)
        {
            return _runningStreams.TryGetValue(streamIdentifier, out result); 
        }

        private StreamManager CreateActiveStream(Exchange exchange,
            CancellationToken callerCancellationToken,
            SemaphoreSlim ongoingStreamInit, CancellationTokenSource resetTokenSource)
        {
            if (_onError)
                throw new InvalidOperationException("This connection is on error");

            ongoingStreamInit.Wait(callerCancellationToken);

            var streamId = Interlocked.Add(ref _nextStreamIdentifier, 2);

            var activeStream = new StreamManager(streamId, this, exchange, resetTokenSource); 
            
            _runningStreams[streamId] = activeStream;

            Context.Logger.Trace(exchange, "Affecting streamIdentifier", streamIdentifier: streamId);

            return activeStream;
        }
        
        /// <summary>
        /// Get or create  active stream 
        /// </summary>
        /// <returns></returns>
        public async Task<StreamManager> CreateNewStreamProcessing(Exchange exchange,
            CancellationToken callerCancellationToken, SemaphoreSlim ongoingStreamInit, CancellationTokenSource resetTokenSource)
        {
            if (_onError)
                throw new InvalidOperationException("This connection is on error");
            
            await _maxConcurrentStreamBarrier.WaitAsync(callerCancellationToken).ConfigureAwait(false);
            var res = CreateActiveStream(exchange, callerCancellationToken, ongoingStreamInit, resetTokenSource);

            return res;
        }
        
        public void NotifyDispose(StreamManager streamManager)
        {
            // reset can happens here 

            if (_runningStreams.Remove(streamManager.StreamIdentifier))
            {
                _maxConcurrentStreamBarrier.Release();
                streamManager.Dispose();
            }
        }

        internal Exception GoAwayException { get; private set; }

        public void OnGoAway(Exception ex)
        {
            _onError = ex != null; 
            GoAwayException = ex; 
        }

        public int ShouldWindowUpdate(int dataLength)
        {
            var windowIncrement = 0;

            _overallWindowSize += dataLength;

            if (_overallWindowSize > (0.5 * Context.Setting.Local.WindowSize))
            {
                windowIncrement = _overallWindowSize;

                _overallWindowSize = 0;
            }

            return windowIncrement; 
        }

        public void Dispose()
        {
            _maxConcurrentStreamBarrier.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await _fifoLock.DisposeAsync().ConfigureAwait(false);
        }
    }
}