// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net;
using System.Threading.Tasks;

namespace Fluxzy
{
    public interface ICaptureContext : IAsyncDisposable
    {
        Task Start();

        void Include(IPAddress remoteAddress, int remotePort);

        long Subscribe(string outFileName, IPAddress remoteAddress, int remotePort, int localPort);

        void StoreKey(string nssKey, IPAddress remoteAddress, int remotePort, int localPort);

        void ClearAll();

        void Flush();

        ValueTask Unsubscribe(long subscription);
    }
}
