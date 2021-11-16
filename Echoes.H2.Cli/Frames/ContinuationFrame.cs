using System;
using System.IO;

namespace Echoes.H2.Cli
{
    public readonly struct ContinuationFrame : IBodyFrame, IHeaderHolderFrame
    {
        public ContinuationFrame(Memory<byte> bodyBytes, bool endHeader)
        {
            EndHeader = endHeader;
            Data = bodyBytes;
            BodyLength = Data.Length;
        }

        public bool EndHeader { get; }

        public Memory<byte> Data { get; }

        public void Write(Stream stream)
        {
        }

        public int BodyLength { get; }
    }
}