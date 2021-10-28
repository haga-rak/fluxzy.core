using System.Buffers.Binary;
using System.IO;

namespace Echoes.H2.Cli
{
    public readonly struct WindowUpdateFrame : IBodyFrame
    {
        public WindowUpdateFrame(byte [] data)
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

        public int Length => 4;

        public H2FrameType Type => H2FrameType.WindowUpdate;

        public void Write(Stream stream)
        {
            stream.BuWrite_1_31(Reserved, WindowSizeIncrement);
        }
    }
}