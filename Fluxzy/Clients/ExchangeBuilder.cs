// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients.H11;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Core;
using Fluxzy.Misc.Streams;
using Fluxzy.Rules.Filters;
using CombinedReadonlyStream = Fluxzy.Misc.Streams.CombinedReadonlyStream;

namespace Fluxzy.Clients
{
    public interface ILink 
    {
        Stream ReadStream { get; }

        Stream WriteStream { get; }
    }

    public interface ILocalLink : ILink
    {

    }

    public interface IRemoteLink : ILink
    {

    }

    public class ExchangeBuildingResult : ILocalLink
    {
        private static int _count = 0; 

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

            if (DebugContext.EnableNetworkFileDump)
            {
                ReadStream = new DebugFileStream($"raw/{Id:0000}_browser_",
                    ReadStream, true);

                WriteStream = new DebugFileStream($"raw/{Id:0000}_browser_",
                    WriteStream,false);
            }
        }

        public int Id { get; }

        public Authority Authority { get;  }

        public Stream ReadStream { get; }

        public Stream WriteStream { get; }

        public Exchange ProvisionalExchange { get; }

        public bool TunnelOnly { get;  }
        
    }

    internal class ExchangeBuilder
    {
        private readonly SecureConnectionUpdater _secureConnectionUpdater;
        private readonly Http11Parser _http11Parser;
        private readonly IIdProvider _idProvider;

        private static string AcceptTunnelResponseString = "HTTP/1.1 200 OK\r\nContent-length: 0\r\nConnection: keep-alive\r\n\r\n";

        private static readonly byte [] AcceptTunnelResponse =
            Encoding.ASCII.GetBytes(AcceptTunnelResponseString);

        public ExchangeBuilder(
            SecureConnectionUpdater secureConnectionUpdater,
            Http11Parser http11Parser, 
            IIdProvider idProvider)
        {
            _secureConnectionUpdater = secureConnectionUpdater;
            _http11Parser = http11Parser;
            _idProvider = idProvider;
        }

        public async Task<ExchangeBuildingResult> InitClientConnection(
            Stream stream,
            byte [] buffer,
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

            Encoding.ASCII.GetChars(new Memory<byte>(buffer, 0, blockReadResult.HeaderLength).Span,
                plainHeaderChars);

            var plainHeader = new RequestHeader(plainHeaderChars, true, _http11Parser);

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

                await plainStream.WriteAsync(new ReadOnlyMemory<byte>(AcceptTunnelResponse),
                    token);

                var exchangeContext = new ExchangeContext(authority);

                await runtimeSetting.EnforceRules(exchangeContext, FilterScope.OnAuthorityReceived);
                
                if (exchangeContext.BlindMode)
                {
                    return
                        new ExchangeBuildingResult(
                            authority, plainStream, plainStream,
                            Exchange.CreateUntrackedExchange(_idProvider, exchangeContext,
                                authority, plainHeaderChars, null,
                                AcceptTunnelResponseString.AsMemory(),
                                null, false, _http11Parser, 
                                "HTTP/1.1",
                                receivedFromProxy), true);
                }

                // TODO : Create an Exchange representing the CONNECT REQUEST

                // THIS LINE WILL THROWS IF CLIENT OPEN A TUNNEL FOR A WEBSOCKET PLAIN REQUEST

                var certStart = ITimingProvider.Default.Instant();
                var certEnd = ITimingProvider.Default.Instant();

                var authenticateResult = await _secureConnectionUpdater.AuthenticateAsServer(
                    plainStream, authority.HostName, token);

                var exchange = Exchange.CreateUntrackedExchange(_idProvider, exchangeContext,
                    authority, plainHeaderChars, null,
                    AcceptTunnelResponseString.AsMemory(),
                    null, false, _http11Parser, "HTTP/1.1", receivedFromProxy);

                exchange.Metrics.CreateCertStart = certStart; 
                exchange.Metrics.CreateCertEnd = certEnd; 

                return 
                    new ExchangeBuildingResult
                    (authority,
                        authenticateResult.InStream,
                        authenticateResult.OutStream, exchange, false);
            }

            // Plain request 

            if (!Uri.TryCreate(plainHeader.Path.ToString(), UriKind.Absolute, out var uri))
                return null; // UNABLE TO READ URI FROM CLIENT

            var plainAuthority = new Authority(uri.Host, uri.Port, false);

            var plainExchangeContext = new ExchangeContext(plainAuthority);

            await runtimeSetting.EnforceRules(plainExchangeContext, FilterScope.OnAuthorityReceived);

            return new ExchangeBuildingResult(
                plainAuthority, 
                plainStream, 
                plainStream, 
                new Exchange(_idProvider,
                    plainExchangeContext,
                    plainAuthority, 
                    plainHeader, plainHeader.ContentLength > 0
                    ? new ContentBoundStream(plainStream, plainHeader.ContentLength)
                    : StreamUtils.EmptyStream, "HTTP/1.1", receivedFromProxy), false); 
        }

        public async Task<Exchange> ReadExchange(
            Stream inStream, Authority authority, byte[] buffer,
            ProxyRuntimeSetting runTimeSetting,
            CancellationToken token)
        {
            var blockReadResult = await
                Http11HeaderBlockReader.GetNext(inStream, buffer, () => { }, () => { }, false, token);

            if (blockReadResult.TotalReadLength == 0)
                return null;

            var receivedFromProxy = ITimingProvider.Default.Instant();

            var secureHeaderChars = new char[blockReadResult.HeaderLength];

            Encoding.ASCII.GetChars(new Memory<byte>(buffer, 0, blockReadResult.HeaderLength).Span,
                secureHeaderChars);

            var secureHeader = new RequestHeader(secureHeaderChars, true, _http11Parser);

            if (blockReadResult.TotalReadLength > blockReadResult.HeaderLength)
            {
                inStream = new CombinedReadonlyStream(false,
                    new MemoryStream(buffer, blockReadResult.HeaderLength,
                        blockReadResult.TotalReadLength - blockReadResult.HeaderLength),
                    inStream); 
            }

            var exchangeContext = new ExchangeContext(authority);
            await runTimeSetting.EnforceRules(exchangeContext, FilterScope.OnAuthorityReceived);

            return new Exchange(_idProvider,
                exchangeContext, authority, secureHeader,
                secureHeader.ContentLength > 0
                    ? new ContentBoundStream(inStream, secureHeader.ContentLength)
                    : StreamUtils.EmptyStream, null, receivedFromProxy
            );
        }
    }
}