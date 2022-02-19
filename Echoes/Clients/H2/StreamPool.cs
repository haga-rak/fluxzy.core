using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Helpers;

namespace Echoes.H2
{
  

    internal class StreamPool :  IDisposable, IAsyncDisposable
    {
        private readonly PeerSetting _remotePeerSetting;
        private readonly H2Logger _logger;
        private readonly StreamProcessingBuilder _streamProcessingBuilder;
        private readonly H2StreamSetting _setting;

        private readonly IDictionary<int, StreamManager> _runningStreams = new Dictionary<int, StreamManager>();

        private int _nextStreamIdentifier = -1;

        private readonly SemaphoreSlim _barrier;
        private readonly SemaphoreSlim _lockker = new(1);
        private bool _onError;

        private readonly FifoLock _fifoLock = new FifoLock();
        private int _overallWindowSize;

        public StreamPool(
            int connectionId, 
            Authority authority, 
            H2Logger logger,
            StreamProcessingBuilder streamProcessingBuilder,
            H2StreamSetting setting)
        {
            ConnectionId = connectionId;
            Authority = authority;
            _logger = logger;
            _streamProcessingBuilder = streamProcessingBuilder;
            _setting = setting;
            _remotePeerSetting = setting.Remote;
            _barrier = new SemaphoreSlim((int)setting.Remote.SettingsMaxConcurrentStreams);

            _overallWindowSize  = setting.Local.WindowSize - setting.Local.MaxFrameSize;
        }

        public int ConnectionId { get; }

        public Authority Authority { get; }
        
        public bool TryGetExistingActiveStream(int streamIdentifier, out StreamManager result)
        {
            return _runningStreams.TryGetValue(streamIdentifier, out result); 
        }

        private StreamManager CreateActiveStream(Exchange exchange, CancellationToken callerCancellationToken, SemaphoreSlim ongoingStreamInit)
        {
            if (_onError)
                throw new InvalidOperationException("This connection is on error");

            ongoingStreamInit.Wait(callerCancellationToken);

            var myId = Interlocked.Add(ref _nextStreamIdentifier, 2);

            StreamManager activeStream = _streamProcessingBuilder.Build(
                myId, this, exchange, _logger,
                callerCancellationToken);
            
            _runningStreams[myId] = activeStream;

            _logger.Trace(exchange, "Affecting streamIdentifier", streamIdentifier: myId);

            return activeStream;
        }
        
        /// <summary>
        /// Get or create  active stream 
        /// </summary>
        /// <returns></returns>
        public async Task<StreamManager> CreateNewStreamProcessing(Exchange exchange, CancellationToken callerCancellationToken, SemaphoreSlim ongoingStreamInit)
        {
            if (_onError)
                throw new InvalidOperationException("This connection is on error");
            
            await _barrier.WaitAsync(callerCancellationToken).ConfigureAwait(false);
            var res = CreateActiveStream(exchange, callerCancellationToken, ongoingStreamInit);

            return res;
        }
        
        public void NotifyDispose(StreamManager streamManager)
        {
            if (_runningStreams.Remove(streamManager.StreamIdentifier))
            {
                _barrier.Release();
                streamManager.Dispose();
            }
        }

        public string WindowSizeStatus()
        {
            return string.Join(",",
                _runningStreams.Values.ToList().OrderBy(r => r.StreamIdentifier)
                    .Select(s => $"({s.StreamIdentifier} , {s.RemoteWindowSize.WindowSize})")); 
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

            if (_overallWindowSize > (0.5 * _setting.Local.WindowSize))
            {
                windowIncrement = _overallWindowSize;

                _overallWindowSize = 0;
            }

            return windowIncrement; 
        }

        public void Dispose()
        {
            _barrier.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await _fifoLock.DisposeAsync().ConfigureAwait(false);
        }
    }
}