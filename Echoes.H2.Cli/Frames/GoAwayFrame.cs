using System;
using System.Buffers.Binary;
using Echoes.H2.Cli.Helpers;

namespace Echoes.H2.Cli
{
    public readonly ref struct GoAwayFrame
    {
        public GoAwayFrame(ReadOnlySpan<byte> bodyBytes)
        {
            LastStreamId = BinaryPrimitives.ReadInt32BigEndian(bodyBytes);
            ErrorCode = (H2ErrorCode) BinaryPrimitives.ReadInt32BigEndian(bodyBytes.Slice(4));
            BodyLength = bodyBytes.Length;
        }

        public int LastStreamId { get; }

        public H2ErrorCode ErrorCode { get; }

        public int Write(Span<byte> buffer)
        {
            var offset =
                H2Frame.Write(buffer, BodyLength, H2FrameType.Goaway, HeaderFlags.None, 0);

            buffer.Slice(offset)
                .BuWrite_32(LastStreamId)
                .BuWrite_32((int)ErrorCode);

            return 9 + 8;
        }

        public int BodyLength { get;  }


    }
}