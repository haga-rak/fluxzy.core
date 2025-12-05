// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using Fluxzy.Misc;

namespace Fluxzy.Clients.H2.Frames
{
    public readonly ref struct GoAwayFrame
    {
        public GoAwayFrame(ReadOnlySpan<byte> bodyBytes)
        {
            LastStreamId = BinaryPrimitives.ReadInt32BigEndian(bodyBytes);
            ErrorCode = (H2ErrorCode) BinaryPrimitives.ReadInt32BigEndian(bodyBytes.Slice(4));
        }

        public GoAwayFrame(int lastStreamId, H2ErrorCode errorCode)
            : this()
        {
            LastStreamId = lastStreamId;
            ErrorCode = errorCode;
        }

        public int LastStreamId { get; }

        public H2ErrorCode ErrorCode { get; }

        public int Write(Span<byte> buffer)
        {
            var offset =
                H2Frame.Write(buffer, BodyLength, H2FrameType.Goaway, HeaderFlags.None, 0);

            buffer.Slice(offset)
                  .BuWrite_32(LastStreamId)
                  .BuWrite_32((int) ErrorCode);

            return 9 + 8;
        }

        public int BodyLength => 8;

        public void Read(out H2ErrorCode errorCode, out int lastStreamId)
        {
            errorCode = ErrorCode;
            lastStreamId = LastStreamId;
        }
    }
}
