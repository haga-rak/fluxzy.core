// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Clients.H2
{
    public class H2FrameReader
    {
        public static H2FrameReadResult ReadFrame(ref ReadOnlyMemory<byte> inputBuffer)
        {
            var headerBuffer = inputBuffer.Slice(0, 9);
            var frame = new H2Frame(headerBuffer.Span);

            var bodyBuffer = inputBuffer.Slice(0, frame.BodyLength);

            return new H2FrameReadResult(frame, bodyBuffer);
        }
    }
}
