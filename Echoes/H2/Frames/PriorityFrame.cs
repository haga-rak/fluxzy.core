using System;
using System.Buffers.Binary;
using Echoes.Helpers;

namespace Echoes.H2
{
    public readonly ref struct PriorityFrame
    {
        public PriorityFrame(ReadOnlySpan<byte> data, int streamIdentifier)
        {
            StreamIdentifier = streamIdentifier;
            Exclusive = (data[0] >> 7) == 1;
            StreamDependency = BinaryPrimitives.ReadInt32BigEndian(data) & 0x7FFFFFFF;
            Weight = data[4];
        }

        public int StreamIdentifier { get; }

        public bool Exclusive { get; }

        public int StreamDependency { get; }

        public byte Weight { get; }

        public int BodyLength => 5;
        
        public int Write(Span<byte> buffer)
        {
            var offset =
                H2Frame.Write(buffer, BodyLength, H2FrameType.Priority, HeaderFlags.None, StreamIdentifier);

            buffer = buffer.Slice(offset).BuWrite_1_31(Exclusive, StreamDependency);
            buffer = buffer.BuWrite_8(Weight);

            return 9 + 5;
        }

    }
}