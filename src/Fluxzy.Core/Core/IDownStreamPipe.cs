// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.ResizableBuffers;

namespace Fluxzy.Core
{
    public interface IDownStreamPipe : IDisposable
    {
        Authority RequestedAuthority { get; }

        bool TunnelOnly { get; }

        ValueTask<Exchange?> ReadNextExchange(RsBuffer buffer, ExchangeScope exchangeScope, CancellationToken token);

        ValueTask WriteResponseHeader(ResponseHeader responseHeader, RsBuffer buffer, bool shouldClose, int streamIdentifier, ReadOnlyMemory<char> requestMethod, CancellationToken token);

        ValueTask WriteResponseBody(Stream responseBodyStream, RsBuffer rsBuffer, bool chunked, int streamIdentifier, Response? responseForTrailers, CancellationToken token);

        /// <summary>
        ///     Write an interim (1xx) response to the downstream client. Used to
        ///     forward an upstream `100 Continue` back to a client that sent
        ///     `Expect: 100-continue` (issue #624).
        /// </summary>
        ValueTask WriteInterimResponse(int statusCode, ReadOnlyMemory<char> reasonPhrase, int streamIdentifier, CancellationToken token);

        (Stream ReadStream, Stream WriteStream) AbandonPipe();

        bool CanWrite { get; }

        bool SupportsMultiplexing { get; }
    }
}
