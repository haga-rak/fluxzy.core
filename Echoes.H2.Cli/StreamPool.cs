using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2.Cli
{
    internal class StreamPool :  IDisposable
    {
        private readonly PeerSetting _remotePeerSetting;
        private readonly IActiveStreamBuilder _activeStreamBuilder;
        
        private readonly IDictionary<int, StreamActivity> _runningStreams = new Dictionary<int, StreamActivity>();

        private int _nextStreamIdentifier = 1;

        private readonly SemaphoreSlim _barrier; 

        public StreamPool(
            IActiveStreamBuilder activeStreamBuilder,
            PeerSetting remotePeerSetting)
        {
            _remotePeerSetting = remotePeerSetting;
            _barrier = new SemaphoreSlim((int) remotePeerSetting.SettingsMaxConcurrentStreams);

        }

        public bool TryGetExistingActiveStream(int streamIdentifier, out StreamActivity result)
        {
            return _runningStreams.TryGetValue(streamIdentifier, out result); 
        }

        private StreamActivity CreateActiveStream()
        {
            var activeStream = _activeStreamBuilder.Build(_nextStreamIdentifier, _remotePeerSetting);

            _runningStreams[_nextStreamIdentifier] = activeStream;

            Interlocked.Add(ref _nextStreamIdentifier, 2);

            return activeStream;
        }
        
        /// <summary>
        /// Get or create  active stream 
        /// </summary>
        /// <returns></returns>
        public async Task<StreamActivity> CreateNewStreamActivity(CancellationToken token)
        {
            await _barrier.WaitAsync(token).ConfigureAwait(false); 
            return CreateActiveStream(); 
        }

        public void NotifyDispose(StreamActivity streamActivity)
        {
            if (_runningStreams.Remove(streamActivity.StreamIdentifier))
                _barrier.Release();

        }

        public void Dispose()
        {
            _barrier.Dispose();
        }
    }
    
    internal interface IActiveStreamBuilder
    {
        StreamActivity Build(int streamIdentifier, PeerSetting remotePeerSetting); 
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