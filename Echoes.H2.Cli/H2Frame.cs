using System;
using System.Buffers.Binary;
using System.IO;

namespace Echoes.H2.Cli
{
    public readonly struct H2Frame : IFixedSizeFrame 
    {
        private H2Frame(int length, H2FrameType bodyType, byte flags, int streamIdentifier)
        {
            Length = length;
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

            Length = headerFrames[2] | headerFrames[1] << 8 | headerFrames[0] << 16;  // 24 premier bits == length 
            BodyType = (H2FrameType) headerFrames[3];
            Flags = headerFrames[4];
            StreamIdentifier = BinaryPrimitives.ReadInt32BigEndian(headerFrames.Slice(5, 4));
        }

        public int Length { get; }

        public H2FrameType BodyType { get; }

        public byte Flags { get; }

        public int StreamIdentifier { get;  }

        public void Write(Stream stream)
        {
            stream.BuWrite_24(Length);
            stream.BuWrite_8((byte) BodyType);
            stream.BuWrite_8(Flags);
            stream.BuWrite_32(StreamIdentifier);
        }

        private static void Write(Stream stream, int length, H2FrameType type, int streamIdentifier = 0,
            byte flags = 0)
        {
            var header = new H2Frame(length, type, flags, streamIdentifier); 
            header.Write(stream);
        }
        

    }
}