// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes
{
    /// <summary>
    /// Represents a connection pool to the same authority.
    /// </summary>
    public interface IHttpConnectionPool : IAsyncDisposable, IDisposable
    {
        Authority Authority { get; }
        
        Task Init();
        
        ValueTask Send(Exchange exchange, ILocalLink localLink, CancellationToken cancellationToken = default);
    }
}