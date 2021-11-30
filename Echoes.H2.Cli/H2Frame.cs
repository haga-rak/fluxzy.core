using System;
using System.Buffers.Binary;
using Echoes.H2.Cli.Helpers;

namespace Echoes.H2.Cli
{
    public readonly ref struct H2Frame 
    {
        public H2Frame(int length, H2FrameType bodyType, HeaderFlags flags, int streamIdentifier)
        {
            BodyLength = length;
            BodyType = bodyType;
            Flags = flags;
            StreamIdentifier = streamIdentifier;
        }

        public H2Frame(ReadOnlySpan<byte> headerFrames)
        {
            if (headerFrames.Length != (9))
            {
                throw new InvalidOperationException("Header length should always 9 octets. Hasta la vista!"); 
            }

            BodyLength = headerFrames[2] | headerFrames[1] << 8 | headerFrames[0] << 16;  // 24 premier bits == length 
            BodyType = (H2FrameType) headerFrames[3];
            Flags = (HeaderFlags) headerFrames[4];
            StreamIdentifier = BinaryPrimitives.ReadInt32BigEndian(headerFrames.Slice(5, 4));
        }

        public int BodyLength { get; }

        public H2FrameType BodyType { get; }

        public HeaderFlags Flags { get; }

        public int StreamIdentifier { get;  }

        public int Write(Span<byte> data)
        {
            data
                .BuWrite_24(BodyLength)
                .BuWrite_8((byte) BodyType)
                .BuWrite_8((byte) Flags)
                .BuWrite_32(StreamIdentifier);

            return 9; 
        }

        public static int Write(Span<byte> buffer, int length, H2FrameType bodyType, HeaderFlags flags, int streamIdentifier)
        {
            var frame = new H2Frame(length, bodyType, flags, streamIdentifier);
            return frame.Write(buffer); 
        }
        
        public static H2Frame BuildDataFrameHeader(int length, int streamIdentifier)
        {
            return new H2Frame(length, H2FrameType.Data, HeaderFlags.None, streamIdentifier); 
        }

        public static H2Frame BuildHeaderFrameHeader(int length, int streamIdentifier, bool first, bool endStream, bool endHeader)
        {
            HeaderFlags flags = 0;

            if (endStream)
                flags |= HeaderFlags.EndStream;
            if (endHeader)
                flags |= HeaderFlags.EndHeaders;

            return new H2Frame(length, first ? H2FrameType.Headers : H2FrameType.Continuation, flags, streamIdentifier);
        }
    }

    [Flags]
    public enum HeaderFlags : byte
    {
        None = 0x0,
        Ack = 0x1,
        EndStream = 0x1 ,
        EndHeaders = 0x4,
        Padded = 0x8,
        Priority = 0x20

    }
}