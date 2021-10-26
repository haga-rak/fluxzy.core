using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2.Cli
{
    public interface IFixedSizeFrame
    {
        void Write(Stream stream);

        int Length { get; }

        H2FrameType Type { get; }
    }
}