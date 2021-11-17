using System;
using System.IO;

namespace Echoes.H2.Cli
{
    public readonly struct DataFrame : IBodyFrame
    {
        public DataFrame(Memory<byte> bodyBytes, bool padded, bool endStream)
        {
            EndStream = endStream;
            var paddedLength = 0;

            if (padded)
            {
                paddedLength = bodyBytes.Span[0];
                bodyBytes = bodyBytes.Slice(1);
            }

            BodyLength = bodyBytes.Length - paddedLength;
            Buffer = bodyBytes.Slice(0, BodyLength);
        }

        public bool EndStream { get; }

        public Memory<byte> Buffer { get; }

        public int BodyLength { get; }

        public void Write(Stream stream)
        {
        }

    }
}