// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core.Pcap.Pcapng
{
    internal abstract class SleepyStreamBlockReader<T> : IBlockReader<T>, IAsyncDisposable
    {
        private readonly SleepyStream _sleepyStream;
        private T ? _nextBlock = default;

        private bool _eof;

        protected SleepyStreamBlockReader(Func<Stream> streamFactory)
        {
            _sleepyStream = new SleepyStream(streamFactory);
        }

        protected abstract T? ReadNextBlock(SleepyStream stream);

        protected abstract int ReadTimeStamp(T block);

        private T? InternalReadNextBlock()
        {
            if (_nextBlock != null) {
                return _nextBlock;
            }

            if (_eof)
                return default; 

            _nextBlock = ReadNextBlock(_sleepyStream);

            _eof = _nextBlock == null;

            return _nextBlock; 
        }

        public int? NextTimeStamp {
            get
            {
                if (_eof) {
                    return null; 
                }

                var block = InternalReadNextBlock();

                if (block != null) {
                    return ReadTimeStamp(block);
                }

                return null;
            }
        }

        public T? Dequeue()
        {
            var result = InternalReadNextBlock();

            _nextBlock = default; 
            return result; 
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
}
