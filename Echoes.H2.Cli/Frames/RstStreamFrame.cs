using System;
using System.Buffers.Binary;
using System.IO;
using Echoes.H2.Cli.Helpers;

namespace Echoes.H2.Cli
{
    public readonly struct RstStreamFrame : IBodyFrame
    {
        public RstStreamFrame(ReadOnlySpan<byte> bodyBytes)
        {
            ErrorCode = BinaryPrimitives.ReadInt32BigEndian(bodyBytes); 
        }

        public int ErrorCode { get;  }

        public int BodyLength => 4;

        public void Write(Stream stream)
        {
            stream.BuWrite_32(ErrorCode);
        }
    }
}