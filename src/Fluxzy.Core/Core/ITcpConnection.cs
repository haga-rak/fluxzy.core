// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Core
{
    public interface ITcpConnection : IAsyncDisposable
    {
        Task<ITcpConnectionConnectResult> ConnectAsync(IPAddress address, int port);

        Task<ITcpConnectionConnectResult> ConnectAsync(IPAddress address, int port, UpstreamConnectOptions options)
            => ConnectAsync(address, port);

        Task<ITcpConnectionConnectResult> ConnectAsync(
            IPAddress address, int port, UpstreamConnectOptions options, CancellationToken token)
            => ConnectAsync(address, port, options);
    }

    public interface ITcpConnectionConnectResult
    {
        DisposeEventNotifierStream Stream { get; }

        void ProcessNssKey(string nssKey);
    }

}
