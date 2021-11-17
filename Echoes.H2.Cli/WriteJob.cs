using System;

namespace Echoes.H2.Cli
{
    public readonly struct WriteJob
    {
        public WriteJob(Memory<byte> data, int priority)
        {
            Data = data;
            Priority = priority;
        }

        public Memory<byte> Data { get;  }

        public int Priority { get;  }
    }
}