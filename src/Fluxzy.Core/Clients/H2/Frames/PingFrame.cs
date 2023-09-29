﻿// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using Fluxzy.Misc;

namespace Fluxzy.Clients.H2.Frames
{
    public readonly ref struct PingFrame
    {
        public PingFrame(ReadOnlySpan<byte> bodyBytes, HeaderFlags flags)
        {
            Flags = flags;
            OpaqueData = BinaryPrimitives.ReadInt64BigEndian(bodyBytes);
        }

        public PingFrame(long opaqueData, HeaderFlags flags)
        {
            Flags = flags;
            OpaqueData = opaqueData;
        }

        public HeaderFlags Flags { get; }

        public long OpaqueData { get; }

        public int BodyLength => 8;

        public int Write(Span<byte> buffer)
        {
            var offset =
                H2Frame.Write(buffer, BodyLength, H2FrameType.Ping, Flags, 0);

            buffer.Slice(offset).BuWrite_64(OpaqueData);

            return 9 + BodyLength;
        }
    }
}
