using System;
using System.Buffers.Binary;
using System.IO;
using Echoes.H2.Cli.Helpers;

namespace Echoes.H2.Cli
{
    public interface IPriorityFrame
    {
        bool Exclusive { get; }
        int StreamDependency { get; }
        byte Weight { get; }
    }

    public readonly struct PriorityFrame : IBodyFrame, IPriorityFrame
    {
        public PriorityFrame(ReadOnlySpan<byte> data)
        {
            Exclusive = (data[0] >> 7) == 1;
            StreamDependency = BinaryPrimitives.ReadInt32BigEndian(data) & 0x7FFFFFFF;
            Weight = data[4];
        }

        public bool Exclusive { get; }

        public int StreamDependency { get; }

        public byte Weight { get; }

        public int BodyLength => 5;

        public void Write(Stream stream)
        {
            stream.BuWrite_1_31(Exclusive, StreamDependency);
            stream.BuWrite_8(Weight);
        }

    }
}