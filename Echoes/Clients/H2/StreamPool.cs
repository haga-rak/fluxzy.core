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
        
        private readonly IDictionary<int, StreamProcessing> _runningStreams = new Dictionary<int, StreamProcessing>();

        private int _nextStreamIdentifier = -1;

        private readonly SemaphoreSlim _barrier;
        private readonly SemaphoreSlim _lockker = new(1);
        private bool _onError;

        private readonly FifoLock _fifoLock = new FifoLock();

        public StreamPool(
            int connectionId, 
            Authority authority, 
            H2Logger logger,
            StreamProcessingBuilder streamProcessingBuilder,
            PeerSetting remotePeerSetting)
        {
            ConnectionId = connectionId;
            Authority = authority;
            _logger = logger;
            _streamProcessingBuilder = streamProcessingBuilder;
            _remotePeerSetting = remotePeerSetting;
            _barrier = new SemaphoreSlim((int) remotePeerSetting.SettingsMaxConcurrentStreams);
        }

        public int ConnectionId { get; }

        public Authority Authority { get; }

        public bool TryGetExistingActiveStream(int streamIdentifier, out StreamProcessing result)
        {
            return _runningStreams.TryGetValue(streamIdentifier, out result); 
        }

        public List<int> status = new List<int>();

        private StreamProcessing CreateActiveStream(Exchange exchange, CancellationToken callerCancellationToken, SemaphoreSlim ongoingStreamInit)
        {
            if (_onError)
                throw new InvalidOperationException("This connection is on error");

            ongoingStreamInit.Wait(callerCancellationToken);

            var myId = Interlocked.Add(ref _nextStreamIdentifier, 2);

            StreamProcessing activeStream = _streamProcessingBuilder.Build(
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
        public async Task<StreamProcessing> CreateNewStreamProcessing(Exchange exchange, CancellationToken callerCancellationToken, SemaphoreSlim ongoingStreamInit)
        {
            if (_onError)
                throw new InvalidOperationException("This connection is on error");
            
            await _barrier.WaitAsync(callerCancellationToken).ConfigureAwait(false);
            var res = CreateActiveStream(exchange, callerCancellationToken, ongoingStreamInit);

            return res;
        }
        
        public void NotifyDispose(StreamProcessing streamProcessing)
        {
            if (_runningStreams.Remove(streamProcessing.StreamIdentifier))
            {
                _barrier.Release();
                streamProcessing.Dispose();
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