// Copyright © 2022 Haga RAKOTOHARIVELO

namespace Fluxzy.Clients.H11
{
    public readonly struct HeaderBlockReadResult
    {
        public HeaderBlockReadResult(int headerLength, int totalReadLength)
        {
            HeaderLength = headerLength;
            TotalReadLength = totalReadLength;
        }

        public int HeaderLength { get; }

        public int TotalReadLength { get; }
    }
}
