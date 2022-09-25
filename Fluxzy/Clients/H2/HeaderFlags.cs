using System;

namespace Fluxzy.Clients.H2
{
    [Flags]
    public enum HeaderFlags : byte
    {
        None = 0x0,
        EndStream = 0x1,
        Ack = 0x1,
        EndHeaders = 0x4,
        Padded = 0x8,
        Priority = 0x20
    }
}