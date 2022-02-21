using System;
using System.Buffers.Binary;
using Echoes.Helpers;

namespace Echoes.H2
{
    public readonly ref struct RstStreamFrame
    {
        public RstStreamFrame(ReadOnlySpan<byte> bodyBytes, int streamIdentifier)
        {
            StreamIdentifier = streamIdentifier;
            ErrorCode = (H2ErrorCode) BinaryPrimitives.ReadInt32BigEndian(bodyBytes);
        }
        
        public RstStreamFrame(int streamIdentifier, H2ErrorCode errorCode)
        {
            StreamIdentifier = streamIdentifier;
            ErrorCode = errorCode;
        }

        public int StreamIdentifier { get; }

        public H2ErrorCode ErrorCode { get;  }

        public int BodyLength => 4;

        public int Write(Span<byte> buffer)
        {
            var offset =
                H2Frame.Write(buffer, BodyLength, H2FrameType.RstStream,  HeaderFlags.None, StreamIdentifier);

            buffer.Slice(offset).BuWrite_32((int) ErrorCode); 

            return 9 + 4;
        }
    }
}