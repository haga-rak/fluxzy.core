// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Clients.H2
{
    public enum H2FrameType : byte
    {
        Data = 0x0,
        Headers = 0x1,
        Priority = 0x2,
        RstStream = 0x3,
        Settings = 0x4,
        PushPromise = 0x5,
        Ping = 0x6,
        Goaway = 0x7,
        WindowUpdate = 0x8,
        Continuation = 0x9
    }
}
