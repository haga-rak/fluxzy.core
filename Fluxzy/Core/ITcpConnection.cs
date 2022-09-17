// Copyright © 2022 Haga Rakotoharivelo

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
    }
}