// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2
{
    public interface IHttpClientConnection : IAsyncDisposable, IDisposable
    {
        Task<H2Message> Send(
            ReadOnlyMemory<char> http11RequestHeader, 
            Stream requestBodyStream,
            long bodyLength = -1,
            CancellationToken cancellationToken = default);
    }
}