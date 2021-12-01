using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2.Cli
{
    internal class StreamPool :  IDisposable
    {
        private readonly PeerSetting _remotePeerSetting;
        private readonly IStreamProcessingBuilder _streamProcessingBuilder;
        
        private readonly IDictionary<int, StreamProcessing> _runningStreams = new Dictionary<int, StreamProcessing>();

        private int _nextStreamIdentifier = 1;

        private readonly SemaphoreSlim _barrier; 

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