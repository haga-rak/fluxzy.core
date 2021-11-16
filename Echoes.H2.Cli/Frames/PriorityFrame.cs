using System;
using System.Buffers.Binary;
using System.IO;
using Echoes.H2.Cli.Helpers;

namespace Echoes.H2.Cli
{
    public readonly struct PriorityFrame : IBodyFrame
    {
        public PriorityFrame(ReadOnlySpan<byte> data)
        {
            Exclusive = (data[0] >> 7) == 1;
            StreamDependency = BinaryPrimitives.ReadUInt32BigEndian(data) & 0x7FFFFFFF;
            Weight = data[4];
        }

        public bool Exclusive { get; }

        public uint StreamDependency { get; }

        public byte Weight { get; }

        public int BodyLength => 5;

        public void Write(Stream stream)
        {
            stream.BuWrite_1_31(Exclusive, StreamDependency);
            stream.BuWrite_8(Weight);
        }

    }
}