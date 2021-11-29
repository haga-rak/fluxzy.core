using System;
using System.IO;

namespace Echoes.H2.Cli
{
    public readonly struct GoAwayFrame : IBodyFrame
    {
        public uint LastStreamId { get; }

        public uint ErrorCode { get; }

        public int Write(Span<byte> buffer)
        {

        }

        public int BodyLength => throw new NotImplementedException();
        
    }
}