using System;

namespace Echoes.Clients.H2.Encoder.Utils.Interfaces
{
    public interface IMemoryProvider<T> : IDisposable
    {
        T [] Allocate(int size);

        Memory<T> Allocate(ReadOnlySpan<T> span);
    }
}