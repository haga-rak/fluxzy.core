// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Misc.ResizableBuffers;

namespace Fluxzy.Clients
{
    /// <summary>
    ///     Represents a connection pool to the same authority, using the same .
    /// </summary>
    public interface IHttpConnectionPool : IAsyncDisposable
    {
        Authority Authority { get; }

        bool Complete { get; }

        void Init();

        ValueTask<bool> CheckAlive();

        ValueTask Send(
            Exchange exchange, ILocalLink localLink, RsBuffer buffer,
            CancellationToken cancellationToken = default);
    }
}
