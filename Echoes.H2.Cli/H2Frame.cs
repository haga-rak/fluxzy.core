using System;
using System.Buffers.Binary;
using System.IO;

namespace Echoes.H2.Cli
{
    public readonly struct H2Frame : IFixedSizeFrame 
    {
        public H2Frame(ReadOnlySpan<byte> headerFrames)
        {
            if (headerFrames.Length != (9))
            {
                throw new InvalidOperationException("Header length should always 9 octets. Hasta la vista!"); 
            }

            Length = headerFrames[2] | headerFrames[1] << 8 | headerFrames[0] << 16;  // 24 premier bits == length 

            Type = (H2FrameType) headerFrames[3];
            Flags = headerFrames[4];
            StreamIdentifier = BinaryPrimitives.ReadInt32BigEndian(headerFrames.Slice(5, 4));
        }

        public int Length { get; }

        public H2FrameType Type { get; }

        public byte Flags { get; }

        public int StreamIdentifier { get;  }

        public void Write(Stream stream)
        {
            stream.BuWrite_24(Length);
            stream.BuWrite_8((byte) Type);
            stream.BuWrite_8(Flags);
            stream.BuWrite_32(StreamIdentifier);
        }
        
    }
}