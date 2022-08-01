﻿// Copyright © 2021 Haga Rakotoharivelo

namespace Fluxzy.Clients.H2.Frames
{
    public enum H2ErrorCode : int
    {
        NoError = 0x0,
        ProtocolError = 0x1,
        InternalError = 0x2,
        FlowControlError = 0x3,
        SettingsTimeout = 0x4,
        StreamClosed = 0x5,
        FrameSizeError = 0x6,
        RefusedStream = 0x7,
        Cancel = 0x8,
        CompressionError = 0x9,
        ConnectError = 0xa,
        EnhanceYourCalm = 0xb,
        InadequateSecurity = 0xc,
        Http11Required = 0xd,
    }
}