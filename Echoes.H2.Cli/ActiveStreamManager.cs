using System;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Echoes.H2.Cli
{
    internal class ActiveStreamManager
    {
        private readonly H2StreamSetting _streamSetting;
        private readonly UpStreamChannel _upStreamChannel;
        private readonly ActiveStream[] _repository; 
        private readonly int _currentStreamCount;

        public ActiveStreamManager(
            H2StreamSetting streamSetting,
            UpStreamChannel upStreamChannel
            )
        {
            _streamSetting = streamSetting;
            _upStreamChannel = upStreamChannel;
            _repository = new ActiveStream[streamSetting.Remote.SettingsMaxConcurrentStreams + 1]; 
        }

        public ChannelReader<Stream> ReadChannel { get; set; }
        

        public ActiveStream GetActiveStream(int streamIdentifier)
        {
            // Todo check if out of bounds 
            var activeStream = _repository[(streamIdentifier - 1) / 2]; 

            if (_repository.TryGetValue(streamIdentifier, out var result))
                return result;

            return null;
        }
            

        /// <summary>
        /// Get or create an active stream 
        /// </summary>
        /// <returns></returns>
        public ActiveStream GetOrCreateActiveStream()
        {
            // Try to create a new stream 
            
            // Find an existing unused stream
            var existing = _repository
                .FirstOrDefault(
                    kp => kp.Value.Type == StreamStateType.Open
                          && !kp.Value.Used
                          && kp.Key % 2 == 1);

            if (existing.Value != null)
            {
                existing.Value.Used = true;
                return existing.Value;
            }
                 // Reuse of an existing stream 

            if ((_currentStreamCount + 1) <= _streamSetting.Remote.SettingsMaxConcurrentStreams)
            {
                // Can create another stream 
                var newStreamIdentifier = _repository.Keys.DefaultIfEmpty(1).Max().NextOdd();

                ActiveStream newChannel = new ActiveStream(newStreamIdentifier, _upStreamChannel, ); 

            }
            else
            {

            }

            // State control must be done here 



            //_repository
            //    .Where(kp => kp.Key % 2 == 1 &&  kp.Value.Type.)

            //if (_repository.TryGetValue(streamIdentifier, out var configuration))
            //{
            //    if (!configuration.Type.HasFlag(StreamStateType.CloseRemote))
            //    {
            //        if (configuration.Type == StreamStateType.Idle)
            //            configuration.Type = StreamStateType.Open;

            //        writer = configuration.Channel.Writer;

            //        return true;
            //    }
            //}

        }
        
    }

    internal class H2StreamConfiguration<T> : IDisposable
    {
        public H2StreamConfiguration()
        {
            Channel = System.Threading.Channels.Channel.CreateUnbounded<T>();
        }
        public Channel<T> Channel { get; }


        public void Dispose()
        {
            Channel.Writer.Complete();
        }
    }

    internal static class IntHelper
    {
        public static int NextOdd(this int value)
        {
            if (value % 2 == 1)
                return value;

            return ++value; 
        }

        public static int NextEven(this int value)
        {
            if (value % 2 == 0)
                return value;

            return ++value; 
        }
    }

    public interface IStreamWritable
    {
        Task CopyTo(Stream inputStream);
    }

    /// <summary>
    /// [1] : Close -  [0] Open 
    /// 0 : reserved local
    /// 1 : reserved remote
    /// 2 : Close local
    /// 3 : Close remote 
    /// 4 : Idle
    /// </summary>
    [Flags]
    public enum StreamStateType : ushort
    {
        Idle = 16 ,
        ReservedLocal = 1 | CloseRemote,
        ReservedRemote = 2 | CloseLocal,
        Open = 0,
        CloseLocal = 4,
        CloseRemote = 8,
        Closed = CloseLocal | CloseRemote 
    }
}