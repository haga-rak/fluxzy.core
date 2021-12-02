using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2
{
    internal class StreamPool :  IDisposable
    {
        private readonly PeerSetting _remotePeerSetting;
        private readonly IStreamProcessingBuilder _streamProcessingBuilder;
        
        private readonly IDictionary<int, StreamProcessing> _runningStreams = new Dictionary<int, StreamProcessing>();

        private int _nextStreamIdentifier = 1;

        private readonly SemaphoreSlim _barrier;
        private bool _onError;

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

        private StreamProcessing CreateActiveStream(CancellationToken callerCancellationToken)
        {
            if (_onError)
                throw new InvalidOperationException("This connection is on error"); 

            var activeStream = _streamProcessingBuilder.Build(_nextStreamIdentifier, this, callerCancellationToken);
            _runningStreams[_nextStreamIdentifier] = activeStream;
            Interlocked.Add(ref _nextStreamIdentifier, 2);

            return activeStream;
        }
        
        /// <summary>
        /// Get or create  active stream 
        /// </summary>
        /// <returns></returns>
        public async Task<StreamProcessing> CreateNewStreamActivity(CancellationToken callerCancellationToken)
        {
            if (_onError)
                throw new InvalidOperationException("This connection is on error");

            await _barrier.WaitAsync(callerCancellationToken).ConfigureAwait(false); 
            return CreateActiveStream(callerCancellationToken); 
        }

        public void NotifyDispose(StreamProcessing streamProcessing)
        {
            if (_runningStreams.Remove(streamProcessing.StreamIdentifier))
            {
                _barrier.Release();
                streamProcessing.Dispose();
            }
        }

        public void Dispose()
        {
            _barrier.Dispose();
        }
        

        internal Exception GoAwayException { get; private set; }

        public void OnGoAway(Exception ex)
        {
            _onError = ex != null; 
            GoAwayException = ex; 
        }
    }
    
    public enum StreamStateType : ushort
    {
        Idle = 0 ,
        ReservedLocal ,
        ReservedRemote,
        Open,
        CloseLocal,
        CloseRemote,
        Closed,
        OpenAndFree
    }
}