using System;
using System.Buffers.Binary;
using Echoes.Misc;

namespace Echoes.Clients.H2.Frames
{
    public readonly ref struct WindowUpdateFrame
    {
        public WindowUpdateFrame(ReadOnlySpan<byte> data, int streamIdentifier)
        {
            StreamIdentifier = streamIdentifier;
            WindowSizeIncrement = BinaryPrimitives.ReadInt32BigEndian(data);
            Reserved = false; 
        }

        public WindowUpdateFrame(int windowSizeIncrement, int streamIdentifier) 
        {
            WindowSizeIncrement = windowSizeIncrement;
            Reserved = false;
            StreamIdentifier = streamIdentifier;
        }

        public int StreamIdentifier { get; }

        public bool Reserved { get; }
        
        public int WindowSizeIncrement { get; }

        public int BodyLength => 4;

        public int Write(Span<byte> buffer)
        {
            var offset =
                H2Frame.Write(buffer, BodyLength, H2FrameType.WindowUpdate, HeaderFlags.None, StreamIdentifier);

            buffer.Slice(offset)
                .BuWrite_32(WindowSizeIncrement);

            return 9 + 4;
        }
    }
}