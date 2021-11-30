using System;
using System.Buffers.Binary;
using System.IO;
using Echoes.H2.Cli.Helpers;

namespace Echoes.H2.Cli
{
    public readonly ref struct RstStreamFrame
    {
        public RstStreamFrame(ReadOnlySpan<byte> bodyBytes, int streamIdentifier)
        {
            StreamIdentifier = streamIdentifier;
            ErrorCode = (H2ErrorCode) BinaryPrimitives.ReadInt32BigEndian(bodyBytes);
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