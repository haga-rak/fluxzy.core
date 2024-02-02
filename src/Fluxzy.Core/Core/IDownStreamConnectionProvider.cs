// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Core
{
    public interface IDownStreamConnectionProvider : IDisposable
    {
        IReadOnlyCollection<IPEndPoint> ListenEndpoints { get; }

        /// <summary>
        ///     UpdateTags le provider
        /// </summary>
        /// <returns></returns>
        IReadOnlyCollection<IPEndPoint> Init(CancellationToken token);


        bool TryGetNextPendingConnection(out TcpClient client);


        ValueTask<TcpClient?> GetNextPendingConnection();
    }
}
