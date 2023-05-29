// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H11;
using Fluxzy.Core;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Streams;
using Fluxzy.Rules;

namespace Fluxzy.Clients
{
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

    public class ExchangeBuildingResult : ILocalLink
    {
        private static int _count;

        public ExchangeBuildingResult(
            Authority authority,
            Stream readStream,
            Stream writeStream,
            Exchange provisionalExchange, bool tunnelOnly)
        {
            Id = Interlocked.Increment(ref _count);
            Authority = authority;
            ReadStream = readStream;
            WriteStream = writeStream;
            ProvisionalExchange = provisionalExchange;
            TunnelOnly = tunnelOnly;

            if (DebugContext.EnableNetworkFileDump) {
                ReadStream = new DebugFileStream($"raw/{Id:0000}_browser_",
                    ReadStream, true);

                WriteStream = new DebugFileStream($"raw/{Id:0000}_browser_",
                    WriteStream, false);
            }
        }

        public int Id { get; }

        public Authority Authority { get; }

        public Exchange ProvisionalExchange { get; }

        public bool TunnelOnly { get; }

        public Stream ReadStream { get; }

        public Stream WriteStream { get; }
    }

    internal class ExchangeBuilder
    {
        private static readonly string AcceptTunnelResponseString =
            "HTTP/1.1 200 OK\r\nContent-length: 0\r\nConnection: keep-alive\r\n\r\n";

        private static readonly byte[] AcceptTunnelResponse =
            Encoding.ASCII.GetBytes(AcceptTunnelResponseString);

        private readonly IIdProvider _idProvider;
        private readonly SecureConnectionUpdater _secureConnectionUpdater;

        public ExchangeBuilder(
            SecureConnectionUpdater secureConnectionUpdater,
            IIdProvider idProvider)
        {
            _secureConnectionUpdater = secureConnectionUpdater;
            _idProvider = idProvider;
        }

        public async ValueTask<ExchangeBuildingResult?> InitClientConnection(
            Stream stream,
            RsBuffer buffer,
            ProxyRuntimeSetting runtimeSetting,
            CancellationToken token)
        {
            var plainStream = stream;

            var blockReadResult = await
                Http11HeaderBlockReader.GetNext(plainStream, buffer, () => { }, () => { }, false, token);

            var receivedFromProxy = ITimingProvider.Default.Instant();

            if (blockReadResult.TotalReadLength == 0)
                return null;

            var plainHeaderChars = new char[blockReadResult.HeaderLength];

            Encoding.ASCII.GetChars(new Memory<byte>(buffer.Buffer, 0, blockReadResult.HeaderLength).Span,
                plainHeaderChars);

            var plainHeader = new RequestHeader(plainHeaderChars, true);

            // Classic TLS Request 

            if (plainHeader.Method.Span.Equals("CONNECT", StringComparison.OrdinalIgnoreCase)) {
                // GET Authority 
                var authorityArray =
                    plainHeader.Path.ToString().Split(new[] {':'}, StringSplitOptions.RemoveEmptyEntries);

                var authority = new Authority
                (authorityArray[0],
                    int.Parse(authorityArray[1]),
                    true);

                await plainStream.WriteAsync(new ReadOnlyMemory<byte>(AcceptTunnelResponse),
                    token);

                var exchangeContext = new ExchangeContext(authority, runtimeSetting.VariableContext);

                await runtimeSetting.EnforceRules(exchangeContext, FilterScope.OnAuthorityReceived);

                if (exchangeContext.BlindMode) {
                    return
                        new ExchangeBuildingResult(
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
                    plainStream, authority.HostName, token);

                var exchange = Exchange.CreateUntrackedExchange(_idProvider, exchangeContext,
                    authority, plainHeaderChars, StreamUtils.EmptyStream,
                    AcceptTunnelResponseString.AsMemory(),
                    StreamUtils.EmptyStream, false, "HTTP/1.1", receivedFromProxy);

                exchange.Metrics.CreateCertStart = certStart;
                exchange.Metrics.CreateCertEnd = certEnd;

                return
                    new ExchangeBuildingResult
                    (authority,
                        authenticateResult.InStream,
                        authenticateResult.OutStream, exchange, false);
            }

            var remainder = blockReadResult.TotalReadLength - blockReadResult.HeaderLength;

            if (remainder > 0) {
                var extraBlock = new byte[remainder];

                buffer.Buffer.AsSpan(blockReadResult.HeaderLength, remainder)
                      .CopyTo(extraBlock);

                plainStream = new RecomposedStream(
                    new CombinedReadonlyStream(true, new MemoryStream(extraBlock), plainStream),
                    plainStream);
            }

            // Plain request 

            if (!Uri.TryCreate(plainHeader.Path.ToString(), UriKind.Absolute, out var uri))
                return null; // UNABLE TO READ URI FROM CLIENT

            var plainAuthority = new Authority(uri.Host, uri.Port, false);

            var plainExchangeContext = new ExchangeContext(plainAuthority, runtimeSetting.VariableContext);

            await runtimeSetting.EnforceRules(plainExchangeContext, FilterScope.OnAuthorityReceived);

            var bodyStream = SetChunkedBody(plainHeader, plainStream);

            return new ExchangeBuildingResult(
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
            else {
                bodyStream = plainHeader.ContentLength > 0
                    ? new ContentBoundStream(plainStream, plainHeader.ContentLength)
                    : StreamUtils.EmptyStream;
            }

            return bodyStream;
        }

        public async ValueTask<Exchange?> ReadExchange(
            Stream inStream, Authority authority, RsBuffer buffer,
            ProxyRuntimeSetting runtimeSetting,
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

            if (blockReadResult.TotalReadLength > blockReadResult.HeaderLength) {
                var copyBuffer = new byte[blockReadResult.TotalReadLength - blockReadResult.HeaderLength];

                Buffer.BlockCopy(buffer.Buffer, blockReadResult.HeaderLength, copyBuffer, 0, copyBuffer.Length);

                inStream = new CombinedReadonlyStream(false,
                    new MemoryStream(copyBuffer),
                    inStream);
            }

            var exchangeContext = new ExchangeContext(authority, runtimeSetting.VariableContext);
            await runtimeSetting.EnforceRules(exchangeContext, FilterScope.OnAuthorityReceived);

            var bodyStream = SetChunkedBody(secureHeader, inStream);

            return new Exchange(_idProvider,
                exchangeContext, authority, secureHeader,
                bodyStream, null!, receivedFromProxy
            );
        }
    }
}
