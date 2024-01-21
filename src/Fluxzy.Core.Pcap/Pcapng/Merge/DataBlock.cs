// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core.Pcap.Pcapng.Merge
{
    internal struct DataBlock
    {
        public DataBlock(long timeStamp, ReadOnlyMemory<byte> data)
        {
            TimeStamp = timeStamp;
            Data = data;
        }

        public long TimeStamp { get; }

        public ReadOnlyMemory<byte> Data { get; }
    }
}
