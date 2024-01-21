// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core.Pcap.Pcapng.Merge
{
    internal readonly struct Option
    {
        public Option()
        {
            HasValue = false;
        }

        public Option(DataBlock value)
        {
            HasValue = true;
            Value = value;
        }

        public bool HasValue { get; }

        public DataBlock Value { get; }
    }
}
