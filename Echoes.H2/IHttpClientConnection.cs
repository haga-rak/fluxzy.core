// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Encoding;

namespace Echoes.H2
{
    /// <summary>
    /// Represents a connection pool to the same end point.
    /// </summary>
    public interface IHttpConnectionPool : IAsyncDisposable, IDisposable
    {
        Authority Authority { get; }
        
        Task Init();
        
        ValueTask Send(Exchange exchange, CancellationToken cancellationToken = default);
    }


    public class PendingRequest
    {
        public PendingRequest(ICollection<HeaderField> requestHeaders)
        {
            RequestHeaders = requestHeaders;
        }

        public ICollection<HeaderField> RequestHeaders { get; }
    }
}