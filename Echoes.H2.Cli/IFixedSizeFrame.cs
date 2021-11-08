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


        H2FrameType Type { get; }
    }
}