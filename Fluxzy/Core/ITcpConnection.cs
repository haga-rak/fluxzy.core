// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Fluxzy.Core
{
    public interface ITcpConnection : IDisposable
    {
        Task ConnectAsync(string remoteHost, int port);

        Task ConnectAsync(IPAddress address, int port);

        Stream GetStream();

        IPEndPoint LocalEndPoint { get;  }
    }
}