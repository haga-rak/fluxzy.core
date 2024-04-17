// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.H11;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Core
{
    /// <summary>
    /// An exchange source provider is responsible for reading exchanges from a stream.
    /// </summary>
    internal abstract class ExchangeSourceProvider
    {
        private readonly IIdProvider _idProvider;

        protected ExchangeSourceProvider(IIdProvider idProvider)
        {
            _idProvider = idProvider;
        }

        /// <summary>
        /// Called to init a first connection 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="contextBuilder"></param>
        /// <param name="localEndpoint"></param>
        /// <param name="token"></param>
        /// <param name="remoteEndPoint"></param>
        /// <returns></returns>
        public abstract ValueTask<ExchangeSourceInitResult?> InitClientConnection(
            Stream stream, RsBuffer buffer, IExchangeContextBuilder contextBuilder, 
            IPEndPoint localEndpoint, IPEndPoint remoteEndPoint,
            CancellationToken token);

        /// <summary>
        /// Read an exchange from the client stream
        /// </summary>
        /// <param name="inStream"></param>
        /// <param name="authority"></param>
        /// <param name="buffer"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public virtual async ValueTask<Exchange?> ReadNextExchange(
            Stream inStream, Authority authority, RsBuffer buffer, IExchangeContextBuilder contextBuilder,
            CancellationToken token)
        { // Every next request after the first one is read from the stream

            var blockReadResult = await
                Http11HeaderBlockReader.GetNext(inStream, buffer, () => { }, () => { }, throwOnError: false, token)
                                       .ConfigureAwait(false);

            if (blockReadResult.TotalReadLength == 0)
                return null;

            var receivedFromProxy = ITimingProvider.Default.Instant();

            var secureHeaderChars = new char[blockReadResult.HeaderLength];

            Encoding.ASCII.GetChars(new Memory<byte>(buffer.Buffer, 0, blockReadResult.HeaderLength).Span,
                secureHeaderChars);

            var secureHeader = new RequestHeader(secureHeaderChars, true);

            if (blockReadResult.TotalReadLength > blockReadResult.HeaderLength)
            {
                var copyBuffer = new byte[blockReadResult.TotalReadLength - blockReadResult.HeaderLength];

                Buffer.BlockCopy(buffer.Buffer, blockReadResult.HeaderLength, copyBuffer, 0, copyBuffer.Length);

                inStream = new CombinedReadonlyStream(false,
                    new MemoryStream(copyBuffer),
                    inStream);
            }

            var exchangeContext = await contextBuilder.Create(authority, authority.Secure).ConfigureAwait(false);

            var bodyStream = SetChunkedBody(secureHeader, inStream);

            return new Exchange(_idProvider,
                exchangeContext, authority, secureHeader,
                bodyStream, null!, receivedFromProxy
            );
        }

        protected static Stream SetChunkedBody(RequestHeader plainHeader, Stream plainStream)
        {
            Stream bodyStream;

            if (plainHeader.ChunkedBody)
                bodyStream = new ChunkedTransferReadStream(plainStream, false);
            else
            {
                bodyStream = plainHeader.ContentLength > 0
                    ? new ContentBoundStream(plainStream, plainHeader.ContentLength)
                    : Stream.Null;
            }

            return bodyStream;
        }
    }
}
