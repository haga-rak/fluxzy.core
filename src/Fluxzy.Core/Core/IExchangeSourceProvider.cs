// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.ResizableBuffers;

namespace Fluxzy.Core
{
    /// <summary>
    /// An exchange source provider is responsible for reading exchanges from a stream.
    /// </summary>
    internal interface IExchangeSourceProvider
    {
        /// <summary>
        /// Called to init a first connection 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="contextBuilder"></param>
        /// <param name="requestedEndpoint"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        ValueTask<ExchangeSourceInitResult?> InitClientConnection(
            Stream stream,
            RsBuffer buffer, 
            IExchangeContextBuilder contextBuilder,
            IPEndPoint requestedEndpoint,
            CancellationToken token);

        /// <summary>
        /// Read an exchange from the client stream
        /// </summary>
        /// <param name="inStream"></param>
        /// <param name="authority"></param>
        /// <param name="buffer"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        ValueTask<Exchange?> ReadNextExchange(
            Stream inStream, Authority authority, RsBuffer buffer,
            IExchangeContextBuilder contextBuilder,
            CancellationToken token);
    }
}
