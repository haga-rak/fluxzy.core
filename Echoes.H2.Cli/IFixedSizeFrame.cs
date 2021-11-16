using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2.Cli
{
    public interface IFixedSizeFrame
    {
        void Write(Stream stream);

        int BodyLength { get; }
    }

    public interface IBodyFrame : IFixedSizeFrame
    {
    }

    public interface IHeaderHolderFrame
    {
        public bool EndHeader { get; }
        public Memory<byte> Data { get; }
    }
}