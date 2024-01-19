// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core.Pcap.Pcapng
{
    internal readonly struct Option<T> where T : struct
    {
        public Option()
        {
            HasValue = false;
        }

        public Option(T value)
        {
            HasValue = true;
            Value = value;
        }

        public bool HasValue { get;  }

        public T Value { get; }
    }

    internal abstract class SleepyStreamBlockReader<T> : IBlockReader<T>, IAsyncDisposable
            where T : struct
    {
        private readonly SleepyStream _sleepyStream;
        private Option<T> _nextBlockOption = new ();

        private bool _eof;

        private int _pendingTimeStamp = -1; 

        protected SleepyStreamBlockReader(
            StreamLimiter streamLimiter, 
            Func<Stream> streamFactory)
        {
            _sleepyStream = new SleepyStream(() => {
                var res = streamFactory();
                streamLimiter.NotifyOpen(this);
                return res;

            });
        }

        protected abstract bool ReadNextBlock(SleepyStream stream, out T result);

        protected abstract int ReadTimeStamp(ref T block);


        private bool InternalReadNextBlock(out T result)
        {
            if (_nextBlockOption.HasValue) {
                result = _nextBlockOption.Value;
                return true; 
            }

            result = default; 

            if (_eof)
                return false;

            if (!ReadNextBlock(_sleepyStream, out var nextBlock)) {
                _eof = true;
                _nextBlockOption = default;
                return false; 
            }

            _nextBlockOption = new Option<T>(nextBlock);

            result = _nextBlockOption.Value;
            
            return true; 
        }

        public int NextTimeStamp {
            get
            {
                if (_eof) {
                    return -1;
                }

                if (_pendingTimeStamp != -1)
                {
                    return _pendingTimeStamp;
                }

                var res = InternalReadNextBlock(out var block);

                if (res) {
                    return _pendingTimeStamp = ReadTimeStamp(ref block);
                }

                return -1;
            }
        }

        public bool Dequeue(out T result)
        {
            var res = InternalReadNextBlock(out result);

            _nextBlockOption = default;
            _pendingTimeStamp = -1;

            return res; 
        }


        public void Sleep()
        {
            _sleepyStream.Sleep();
        }

        public void Dispose()
        {
            _sleepyStream.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await _sleepyStream.DisposeAsync();
        }
    }


    internal class StreamLimiter
    {
        private readonly int _concurrentCount;
        private readonly Queue<IBlockReader> _currentQueue;

        public StreamLimiter(int concurrentCount)
        {
            _concurrentCount = concurrentCount;
            _currentQueue = new(concurrentCount + 32);
        }

        public void NotifyOpen(IBlockReader reader)
        {
            _currentQueue.Enqueue(reader);

            while (_currentQueue.Count > _concurrentCount)
            {
                var toSleep = _currentQueue.Dequeue();

                if (toSleep == reader)
                    continue;

                toSleep.Sleep();
            }

        }
    }
}
