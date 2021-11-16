using System;
using System.Buffers.Binary;
using System.IO;
using Echoes.H2.Cli.Helpers;

namespace Echoes.H2.Cli
{
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

        public void Write(Stream stream)
        {
            stream.BuWrite_1_31(Reserved, WindowSizeIncrement);
        }
    }
}