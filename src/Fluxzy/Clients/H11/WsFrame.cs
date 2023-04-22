// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Clients.H11
{
    public struct WsFrame
    {
        public long PayloadLength { get; set; }

        public WsOpCode OpCode { get; set; }

        public bool FinalFragment { get; set; }

        public uint MaskedPayload { get; set; }
    }
}
