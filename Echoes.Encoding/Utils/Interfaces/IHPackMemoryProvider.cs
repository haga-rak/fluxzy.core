using System;

namespace Echoes.Encoding.Utils.Interfaces
{
    public interface IMemoryProvider<T> : IDisposable
    {
        T [] Allocate(int size);

        Memory<T> Allocate(ReadOnlySpan<T> span);
    }
}