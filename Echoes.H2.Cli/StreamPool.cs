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

        private StreamProcessing CreateActiveStream()
        {
            var activeStream = _streamProcessingBuilder.Build(_nextStreamIdentifier, _remotePeerSetting);

            _runningStreams[_nextStreamIdentifier] = activeStream;

            Interlocked.Add(ref _nextStreamIdentifier, 2);

            return activeStream;
        }
        
        /// <summary>
        /// Get or create  active stream 
        /// </summary>
        /// <returns></returns>
        public async Task<StreamProcessing> CreateNewStreamActivity(CancellationToken token)
        {
            await _barrier.WaitAsync(token).ConfigureAwait(false); 
            return CreateActiveStream(); 
        }

        public void NotifyDispose(StreamProcessing streamProcessing)
        {
            if (_runningStreams.Remove(streamProcessing.StreamIdentifier))
                _barrier.Release();

        }

        public void Dispose()
        {
            _barrier.Dispose();
        }
    }
    

    internal interface IStreamProcessingBuilder
    {
        StreamProcessing Build(int streamIdentifier, PeerSetting remotePeerSetting); 
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