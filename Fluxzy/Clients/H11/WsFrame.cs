// Copyright © 2022 Haga Rakotoharivelo

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
