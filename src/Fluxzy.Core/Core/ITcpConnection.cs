// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Core
{
    public interface ITcpConnection : IAsyncDisposable 
    {
        Task<ITcpConnectionConnectResult> ConnectAsync(IPAddress address, int port);
    }

    public interface ITcpConnectionConnectResult
    {
        DisposeEventNotifierStream Stream { get; }

        void ProcessNssKey(string nssKey);
    }

}
