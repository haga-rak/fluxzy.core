// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core.Pcap.Pcapng.Merge
{
    internal abstract class SleepyStreamBlockReader : IBlockReader, IAsyncDisposable
    {
        private readonly SleepyStream _sleepyStream;
        private Option _nextBlockOption = new();

        private bool _eof;

        private long _pendingTimeStamp = long.MaxValue;

        protected SleepyStreamBlockReader(
            StreamLimiter streamLimiter,
            Func<Stream> streamFactory)
        {
            _sleepyStream = new SleepyStream(() =>
            {
                var res = streamFactory();
                streamLimiter.NotifyOpen(this);
                return res;
            });
        }

        protected abstract bool ReadNextBlock(SleepyStream stream, out DataBlock result);

        private bool InternalReadNextBlock(out DataBlock result)
        {
            if (_nextBlockOption.HasValue)
            {
                result = _nextBlockOption.Value;
                return true;
            }

            result = default;

            if (_eof)
                return false;

            if (!ReadNextBlock(_sleepyStream, out var nextBlock))
            {
                _eof = true;
                _nextBlockOption = default;
                return false;
            }

            _nextBlockOption = new Option(nextBlock);

            result = _nextBlockOption.Value;

            return true;
        }

        public long NextTimeStamp
        {
            get
            {
                if (_eof)
                {
                    return long.MaxValue;
                }

                if (_pendingTimeStamp != long.MaxValue)
                {
                    return _pendingTimeStamp;
                }

                var res = InternalReadNextBlock(out var block);

                if (res)
                {
                    return _pendingTimeStamp = block.TimeStamp;
                }

                return long.MaxValue;
            }
        }

        public bool Dequeue(out DataBlock result)
        {
            var res = InternalReadNextBlock(out result);

            _nextBlockOption = default;
            _pendingTimeStamp = long.MaxValue;

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
            _currentQueue = new(concurrentCount + 4);
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
