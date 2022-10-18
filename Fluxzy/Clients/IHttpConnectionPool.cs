// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Clients
{
    /// <summary>
    /// Represents a connection pool to the same authority, using the same .
    /// </summary>
    public interface IHttpConnectionPool : IAsyncDisposable, IDisposable
    {
        Authority Authority { get; }

        bool Complete { get; }
        
        ValueTask Init();

        ValueTask<bool> CheckAlive();
        
        ValueTask Send(Exchange exchange, ILocalLink localLink, byte[] buffer, CancellationToken cancellationToken = default);
    }
}