// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
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
    /// Offers an implementation of IExchangeSourceProvider
    /// based on proxy CONNECT requests.
    /// Determines automatically if request is clear HTTP, HTTPS or TLS.
    /// Accept only HTTP/1.1 requests.
    /// </summary>
    internal class FromProxyConnectSourceProvider : IExchangeSourceProvider
    {
        private static string AcceptTunnelResponseString { get; }

        private static byte[] AcceptTunnelResponse { get; }
        
        static FromProxyConnectSourceProvider()
        {
            AcceptTunnelResponseString =
                $"HTTP/1.1 200 OK\r\n" +
                $"x-fluxzy-message: enjoy your privacy!\r\n" +
                $"Content-length: 0\r\n" +
                $"Connection: keep-alive\r\n" +
                $"Keep-alive: timeout=5\r\n" +
                $"\r\n";

            AcceptTunnelResponse = Encoding.ASCII.GetBytes(AcceptTunnelResponseString);
        }

        private readonly IIdProvider _idProvider;
        private readonly SecureConnectionUpdater _secureConnectionUpdater;

        public FromProxyConnectSourceProvider(
            SecureConnectionUpdater secureConnectionUpdater,
            IIdProvider idProvider)
        {
            _secureConnectionUpdater = secureConnectionUpdater;
            _idProvider = idProvider;
        }

        public async ValueTask<ExchangeSourceInitResult?> InitClientConnection(
            Stream stream,
            RsBuffer buffer,
            IExchangeContextBuilder contextBuilder,
            CancellationToken token)
        {
            var plainStream = stream;

            var blockReadResult = await
                Http11HeaderBlockReader.GetNext(plainStream, buffer, null, null, false, token);

            var receivedFromProxy = ITimingProvider.Default.Instant();

            if (blockReadResult.TotalReadLength == 0)
                return null;

            var plainHeaderChars = new char[blockReadResult.HeaderLength];

            Encoding.ASCII.GetChars(new Memory<byte>(buffer.Buffer, 0, blockReadResult.HeaderLength).Span,
                plainHeaderChars);

            var plainHeader = new RequestHeader(plainHeaderChars, true);

            // Classic TLS Request 

            if (plainHeader.Method.Span.Equals("CONNECT", StringComparison.OrdinalIgnoreCase))
            {
                // GET Authority 
                var authorityArray =
                    plainHeader.Path.ToString().Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                var authority = new Authority
                (authorityArray[0],
                    int.Parse(authorityArray[1]),
                    true);

                await plainStream.WriteAsync(new ReadOnlyMemory<byte>(AcceptTunnelResponse), token);

                var exchangeContext  = await contextBuilder.Create(authority, true);

                if (exchangeContext.BlindMode)
                {
                    return
                        new ExchangeSourceInitResult(
                            authority, plainStream, plainStream,
                            Exchange.CreateUntrackedExchange(_idProvider, exchangeContext,
                                authority, plainHeaderChars, StreamUtils.EmptyStream,
                                AcceptTunnelResponseString.AsMemory(),
                                StreamUtils.EmptyStream, false,
                                "HTTP/1.1",
                                receivedFromProxy), true);
                }

                var certStart = ITimingProvider.Default.Instant();
                var certEnd = ITimingProvider.Default.Instant();

                var authenticateResult = await _secureConnectionUpdater.AuthenticateAsServer(
                    plainStream, authority.HostName, exchangeContext, token);

                var exchange = Exchange.CreateUntrackedExchange(_idProvider, exchangeContext,
                    authority, plainHeaderChars, StreamUtils.EmptyStream,
                    AcceptTunnelResponseString.AsMemory(),
                    StreamUtils.EmptyStream, false, "HTTP/1.1", receivedFromProxy);

                exchange.Metrics.CreateCertStart = certStart;
                exchange.Metrics.CreateCertEnd = certEnd;

                return
                    new ExchangeSourceInitResult
                    (authority,
                        authenticateResult.InStream,
                        authenticateResult.OutStream, exchange, false);
            }

            var remainder = blockReadResult.TotalReadLength - blockReadResult.HeaderLength;

            if (remainder > 0)
            {
                var extraBlock = new byte[remainder];

                buffer.Buffer.AsSpan(blockReadResult.HeaderLength, remainder)
                      .CopyTo(extraBlock);

                plainStream = new RecomposedStream(
                    new CombinedReadonlyStream(true, new MemoryStream(extraBlock), plainStream),
                    plainStream);
            }

            // Plain request 

            var path = plainHeader.Path.ToString();

            if (!Uri.TryCreate(path, UriKind.Absolute, out var uri)
                || !uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var builder = new StringBuilder("http://");

                builder.Append(plainHeader.Authority.Span);

                if (!path.StartsWith("/"))
                    builder.Append("/");

                builder.Append(path);

                if (!Uri.TryCreate(builder.ToString(), UriKind.Absolute, out uri))
                    return null; // UNABLE TO READ URI FROM CLIENT
            }

            var plainAuthority = new Authority(uri.Host, uri.Port, false);
            var plainExchangeContext = await contextBuilder.Create(plainAuthority, false);

            var bodyStream = SetChunkedBody(plainHeader, plainStream);

            return new ExchangeSourceInitResult(
                plainAuthority,
                plainStream,
                plainStream,
                new Exchange(_idProvider,
                    plainExchangeContext,
                    plainAuthority,
                    plainHeader, bodyStream, "HTTP/1.1", receivedFromProxy), false);
        }

        private static Stream SetChunkedBody(RequestHeader plainHeader, Stream plainStream)
        {
            Stream bodyStream;

            if (plainHeader.ChunkedBody)
                bodyStream = new ChunkedTransferReadStream(plainStream, false);
            else
            {
                bodyStream = plainHeader.ContentLength > 0
                    ? new ContentBoundStream(plainStream, plainHeader.ContentLength)
                    : StreamUtils.EmptyStream;
            }

            return bodyStream;
        }

        public async ValueTask<Exchange?> ReadNextExchange(
            Stream inStream, Authority authority, RsBuffer buffer,
            IExchangeContextBuilder contextBuilder,
            CancellationToken token)
        {
            // Every next request after the first one is read from the stream

            var blockReadResult = await
                Http11HeaderBlockReader.GetNext(inStream, buffer, () => { }, () => { }, false, token);

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

            var exchangeContext =  await contextBuilder.Create(authority, false);

            var bodyStream = SetChunkedBody(secureHeader, inStream);

            return new Exchange(_idProvider,
                exchangeContext, authority, secureHeader,
                bodyStream, null!, receivedFromProxy
            );
        }
    }


    public interface ILink
    {
        Stream? ReadStream { get; }

        Stream? WriteStream { get; }
    }

    public interface ILocalLink : ILink
    {
    }

    public interface IRemoteLink : ILink
    {
    }
}
