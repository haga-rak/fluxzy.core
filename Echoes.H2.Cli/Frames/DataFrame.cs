using System;
using System.IO;

namespace Echoes.H2.Cli
{
    public readonly struct DataFrame : IBodyFrame
    {
        public DataFrame(Memory<byte> bodyBytes, bool padded)
        {
            var paddedLength = 0;

            if (padded)
            {
                paddedLength = bodyBytes.Span[0];
                bodyBytes = bodyBytes.Slice(1);
            }

            BodyLength = bodyBytes.Length - paddedLength;
            Buffer = bodyBytes.Slice(0, BodyLength);
        }

        public void Write(Stream stream)
        {
        }

        public Memory<byte> Buffer { get; }

        public int BodyLength { get; }

        public H2FrameType Type => H2FrameType.Data; 
    }
}