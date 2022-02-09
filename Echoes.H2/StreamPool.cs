using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2
{
  

    internal class StreamPool :  IDisposable, IAsyncDisposable
    {

        private readonly PeerSetting _remotePeerSetting;
        private readonly IStreamProcessingBuilder _streamProcessingBuilder;
        
        private readonly IDictionary<int, StreamProcessing> _runningStreams = new Dictionary<int, StreamProcessing>();

        private int _nextStreamIdentifier = -1;

        private readonly SemaphoreSlim _barrier;
        private readonly SemaphoreSlim _lockker = new SemaphoreSlim(1);
        private bool _onError;

        private readonly FifoLock _fifoLock = new FifoLock();

        public StreamPool(
            IStreamProcessingBuilder streamProcessingBuilder,
            PeerSetting remotePeerSetting)
        {
            _streamProcessingBuilder = streamProcessingBuilder;
            _remotePeerSetting = remotePeerSetting;
            _barrier = new SemaphoreSlim((int) remotePeerSetting.SettingsMaxConcurrentStreams);
        }

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
            StreamProcessing activeStream = _streamProcessingBuilder.Build(myId, this, exchange, callerCancellationToken);
            
            _runningStreams[myId] = activeStream;

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

        public ConcurrentBag<StreamProcessing> DoneStream = new ConcurrentBag<StreamProcessing>();

        public void NotifyDispose(StreamProcessing streamProcessing)
        {
            if (_runningStreams.Remove(streamProcessing.StreamIdentifier))
            {
                _barrier.Release();
                streamProcessing.Dispose();

                DoneStream.Add(streamProcessing);
            }
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