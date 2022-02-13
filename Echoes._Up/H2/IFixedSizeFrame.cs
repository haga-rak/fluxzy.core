using System;

namespace Echoes.H2
{
    public interface IFixedSizeFrame
    {
        int Write(Span<byte> buffer, ReadOnlySpan<byte> payload = default); 

        int BodyLength { get; }
    }

    public interface IBodyFrame : IFixedSizeFrame
    {
        int StreamIdentifier { get;  }
    }

    public interface IHeaderHolderFrame
    {
        public bool EndHeader { get; }

        public ReadOnlyMemory<byte> Data { get; }
    }
}