using System;
using System.Buffers.Binary;
using System.IO;
using Echoes.H2.Cli.Helpers;

namespace Echoes.H2.Cli
{
    public readonly struct GoAwayFrame : IBodyFrame
    {
        public uint LastStreamId { get; }

        public uint ErrorCode { get; }

        public H2FrameType Type => throw new NotImplementedException();

        public int BodyLength => throw new NotImplementedException();

        public void Write(Stream stream)
        {
            throw new NotImplementedException();
        }
    }

    public readonly struct WindowUpdateFrame : IBodyFrame
    {
        public WindowUpdateFrame(ReadOnlySpan<byte> data)
        {
            WindowSizeIncrement = BinaryPrimitives.ReadInt32BigEndian(data);
            Reserved = false; 
        }

        public WindowUpdateFrame(int windowSizeIncrement) 
        {
            WindowSizeIncrement = windowSizeIncrement;
            Reserved = false; 
        }

        public bool Reserved { get; }
        
        public int WindowSizeIncrement { get; }

        public int BodyLength => 4;

        public H2FrameType Type => H2FrameType.WindowUpdate;

        public void Write(Stream stream)
        {
            stream.BuWrite_1_31(Reserved, WindowSizeIncrement);
        }
    }
}