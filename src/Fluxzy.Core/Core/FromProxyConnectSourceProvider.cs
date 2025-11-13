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
using Fluxzy.Utils;

namespace Fluxzy.Core
{
    /// <summary>
    /// Offers an implementation of ExchangeSourceProvider
    /// based on proxy CONNECT requests.
    /// Determines automatically if request is clear HTTP, HTTPS or TLS.
    /// Accept only HTTP/1.1 requests.
    /// </summary>
    internal class FromProxyConnectSourceProvider : ExchangeSourceProvider
    {
        private static byte[] AcceptTunnelResponse { get; } = Encoding.ASCII.GetBytes(ProxyConstants.AcceptTunnelResponseString);
        
        private readonly IIdProvider _idProvider;
        private readonly ProxyAuthenticationMethod _proxyAuthenticationMethod;
        private readonly IExchangeContextBuilder _contextBuilder;
        private readonly SecureConnectionUpdater _secureConnectionUpdater;
        private readonly int _attemptCount;

        public FromProxyConnectSourceProvider(
            SecureConnectionUpdater secureConnectionUpdater,
            IIdProvider idProvider, ProxyAuthenticationMethod proxyAuthenticationMethod,
            IExchangeContextBuilder contextBuilder) :
            base (idProvider)
        {
            _secureConnectionUpdater = secureConnectionUpdater;
            _idProvider = idProvider;
            _proxyAuthenticationMethod = proxyAuthenticationMethod;
            _contextBuilder = contextBuilder;

            _attemptCount = proxyAuthenticationMethod.AuthenticationType == ProxyAuthenticationType.None ? 
                1 : FluxzySharedSetting.ProxyAuthenticationMaxAttempt;
        }
        
        public override async ValueTask<ExchangeSourceInitResult?> InitClientConnection(
            Stream stream,
            RsBuffer buffer,
            IPEndPoint localEndpoint, IPEndPoint remoteEndPoint,
            CancellationToken token)
        {
            var count = Math.Max(_attemptCount, 1);

            while (count-- > 0)
            {
                var result = await InternalInitClientConnection(stream, buffer, _contextBuilder, localEndpoint, remoteEndPoint, token)
                    .ConfigureAwait(false);

                if (result.Retry)
                    continue;

                return result.ExchangeSourceInitResult; 
            }

            return null; // Max attempt was reached
        }

        private async Task<InternalInitClientConnectionResult> InternalInitClientConnection(
            Stream stream, RsBuffer buffer, IExchangeContextBuilder contextBuilder,
            IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, CancellationToken token)
        {
            var readBlockResult = await ReadNextBlock(stream, buffer, token).ConfigureAwait(false);

            if (readBlockResult == null)
                return new (false, null);

            var (blockReadResult, receivedFromProxy, plainHeaderChars,
                plainHeader) = readBlockResult;

            // Classic TLS Request 

            if (plainHeader.Method.Span.Equals("CONNECT", StringComparison.OrdinalIgnoreCase) 
                 && !UrlHelper.IsAbsoluteHttpUrl(plainHeader.Path.Span))
            {
                // GET Authority 
                var authorityArray =
                    plainHeader.Path.ToString().Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                var authority = new Authority(authorityArray[0],  int.Parse(authorityArray[1]), true);

                if (!_proxyAuthenticationMethod.ValidateAuthentication(localEndPoint, remoteEndPoint, plainHeader)) {
                    var rawResponse = _proxyAuthenticationMethod.GetUnauthorizedResponse(localEndPoint, remoteEndPoint, plainHeader);

                    await stream.WriteAsync(rawResponse, token).ConfigureAwait(false);

                    return new(true, null); 
                }

                await stream.WriteAsync(new ReadOnlyMemory<byte>(AcceptTunnelResponse), token);
                var exchangeContext = await contextBuilder.Create(authority, true).ConfigureAwait(false);

                if (exchangeContext.BlindMode) {

                    var provisionalExchange = Exchange.CreateUntrackedExchange(_idProvider, exchangeContext,
                        authority, plainHeaderChars, Stream.Null,
                        ProxyConstants.AcceptTunnelResponseString.AsMemory(),
                        Stream.Null, false,
                        "HTTP/1.1",
                        receivedFromProxy);

                    provisionalExchange.Unprocessed = false;

                    return
                        new (false, new ExchangeSourceInitResult(
                            new Http11DownStreamPipe(_idProvider, authority, stream, stream, _contextBuilder), 
                            provisionalExchange));
                }

                var certStart = ITimingProvider.Default.Instant();
                var certEnd = ITimingProvider.Default.Instant();

                var authenticateResult = await _secureConnectionUpdater.AuthenticateAsServer(
                    stream, authority.HostName, exchangeContext, token).ConfigureAwait(false);

                authority = new Authority(authority.HostName, authority.Port, authenticateResult.IsSsl);

                var exchange = Exchange.CreateUntrackedExchange(_idProvider, exchangeContext,
                    authority, plainHeaderChars, Stream.Null,
                    ProxyConstants.AcceptTunnelResponseString.AsMemory(),
                    Stream.Null, authenticateResult.IsSsl, "HTTP/1.1", receivedFromProxy);

                exchange.Metrics.CreateCertStart = certStart;
                exchange.Metrics.CreateCertEnd = certEnd;

                // TLS

                return
                    new(false,
                        new ExchangeSourceInitResult
                            (new Http11DownStreamPipe(_idProvider, authority,
                                authenticateResult.InStream, authenticateResult.OutStream, _contextBuilder), exchange));
            }

            // Plain request 

            var remainder = blockReadResult.TotalReadLength - blockReadResult.HeaderLength;

            if (remainder > 0)
            {
                stream = new RecomposedStream(
                    new CombinedReadonlyStream(true, buffer.Buffer.AsSpan(blockReadResult.HeaderLength, remainder), stream),
                    stream);
            }

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
                    return new (false, null); // UNABLE TO READ URI FROM CLIENT
            }

            var plainAuthority = new Authority(uri.Host, uri.Port, false);
            var plainExchangeContext = await contextBuilder.Create(plainAuthority, false).ConfigureAwait(false);

            var bodyStream = Http11DownStreamPipe.SetChunkedBody(plainHeader, stream);

            var nextExchange = new Exchange(_idProvider,
                plainExchangeContext,
                plainAuthority,
                plainHeader, bodyStream, "HTTP/1.1", receivedFromProxy);

            return new(false, new ExchangeSourceInitResult(new Http11DownStreamPipe(
                _idProvider, plainAuthority, stream, stream, _contextBuilder), nextExchange));
        }

        private record ReadNextBlockResult(
            HeaderBlockReadResult HeaderBlockReadResult,
            DateTime ReceivedFromProxy,
            char[] PlainHeaderChars,
            RequestHeader RequestHeader);

        private record struct InternalInitClientConnectionResult(bool Retry, ExchangeSourceInitResult? ExchangeSourceInitResult);

        private static async Task<ReadNextBlockResult?>
            ReadNextBlock(Stream stream, RsBuffer buffer,
                CancellationToken token)
        {
            HeaderBlockReadResult blockReadResult = await
                Http11HeaderBlockReader.GetNext(stream, buffer, null, null, throwOnError: false, token)
                                       .ConfigureAwait(false);

            var receivedFromProxy = ITimingProvider.Default.Instant();

            if (blockReadResult.TotalReadLength == 0)
                return null;

            var plainHeaderChars = new char[blockReadResult.HeaderLength];

            Encoding.ASCII.GetChars(new Memory<byte>(buffer.Buffer, 0, blockReadResult.HeaderLength).Span,
                plainHeaderChars);

            var plainHeader = new RequestHeader(plainHeaderChars, true);

            return new(blockReadResult, receivedFromProxy, plainHeaderChars, plainHeader);
        }
    }

    internal static class ProxyConstants
    {
        public static string AcceptTunnelResponseString { get; } =
            $"HTTP/1.1 200 OK\r\n" +
            $"x-fluxzy-message: enjoy your privacy!\r\n" +
            $"\r\n";
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
