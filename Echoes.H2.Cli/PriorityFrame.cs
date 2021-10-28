using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2.Cli
{
    public readonly struct PriorityFrame : IBodyFrame
    {
        public PriorityFrame(byte[] data)
        {
            Exclusive = (data[0] >> 7) == 1;
            StreamDependency = BinaryPrimitives.ReadInt32LittleEndian(data) & 0x7FFFFFFF;
            Weight = data[4];
        }

        public bool Exclusive { get; }

        public int StreamDependency { get; }

        public byte Weight { get; }

        public int Length => 5;

        public H2FrameType Type => H2FrameType.Priority;

        public void Write(Stream stream)
        {
            stream.BuWrite_1_31(Exclusive, StreamDependency);
            stream.BuWrite_8(Weight);
        }

    }
}