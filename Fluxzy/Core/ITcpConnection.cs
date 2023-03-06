// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Fluxzy.Core
{
    public interface ITcpConnection : IAsyncDisposable
    {
        Task<IPEndPoint> ConnectAsync(IPAddress address, int port);

        Stream GetStream();

        void OnKeyReceived(string nssKey);
    }
}
