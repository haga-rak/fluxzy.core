// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Core;
using Echoes.H11;
using Echoes.H2.Encoder.Utils;
using Echoes.IO;

namespace Echoes
{
    public class ExchangeBuildingResult
    {
        private static int Count = 0; 

        public ExchangeBuildingResult(Authority authority, Stream stream , Exchange provisionalExchange)
        {
            Id = Interlocked.Increment(ref Count); 
            Authority = authority;
            Stream = stream;
            ProvisionalExchange = provisionalExchange;

            if (DebugContext.EnableNetworkFileDump)
            {
                Stream = new DebugFileStream($"raw/{Id:0000}_browser_",
                    stream);
            }
        }

        public int Id { get; }

        public Authority Authority { get;  }

        public Stream Stream { get;  }

        public Exchange ProvisionalExchange { get; }
        
    }

    public class ExchangeBuilder
    {
        private readonly ISecureConnectionUpdater _secureConnectionUpdater;
        private readonly Http11Parser _http11Parser;

        private static string AcceptTunnelResponseString = "HTTP/1.1 200 OK\r\nContent-length: 0\r\nConnection: Keep-alive\r\n\r\n";


        private static readonly byte [] AcceptTunnelResponse =
            Encoding.ASCII.GetBytes(AcceptTunnelResponseString);

        public ExchangeBuilder(ISecureConnectionUpdater secureConnectionUpdater,
            Http11Parser http11Parser)
        {
            _secureConnectionUpdater = secureConnectionUpdater;
            _http11Parser = http11Parser;
        }

        public async Task<ExchangeBuildingResult> InitClientConnection(
            Stream stream,
            ProxyStartupSetting startupSetting, 
            CancellationToken token)
        {
            var plainStream = stream;
            var buffer = new byte[startupSetting.MaxHeaderLength];

            var blockReadResult =  await
                Http11PoolProcessing.DetectHeaderBlock(plainStream, buffer, () => { }, () => { }, false, token);

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

                // TODO : Create an Exchange representing the CONNECT REQUEST

                // THIS LINE WILL THROWS IF CLIENT OPEN A TUNNEL FOR A WEBSOCKET PLAIN REQUEST

                var sslStream = await _secureConnectionUpdater.AuthenticateAsServer(
                    plainStream, authority.HostName);

                return 
                    new ExchangeBuildingResult(authority, sslStream, new Exchange(
                    authority, plainHeaderChars, null,
                    AcceptTunnelResponseString.AsMemory(), 
                    null, false, _http11Parser, "HTTP/1.1")
                    {
                        BaseStream = sslStream
                    });
            }

            // Plain request 

            if (!Uri.TryCreate(plainHeader.Path.ToString(), UriKind.Absolute, out var uri))
                return null; // UNABLE TO READ URI FROM CLIENT

            var plainAuthority = new Authority(uri.Host, uri.Port, false);

            return new ExchangeBuildingResult(plainAuthority, plainStream, new Exchange(plainAuthority, 
                plainHeader, plainHeader.ContentLength > 0
                    ? new ContentBoundStream(plainStream, plainHeader.ContentLength)
                    : StreamUtils.EmptyStream, "HTTP/1.1")); 
        }

        public async Task<Exchange> ReadExchange(
            Stream sslStream, Authority authority, byte[] buffer,
            CancellationToken token)
        {
            HeaderBlockReadResult blockReadResult;
            blockReadResult = await
                Http11PoolProcessing.DetectHeaderBlock(sslStream, buffer, () => { }, () => { }, false, token);

            if (blockReadResult.TotalReadLength == 0)
                return null;

            var secureHeaderChars = new char[blockReadResult.HeaderLength];

            Encoding.ASCII.GetChars(new Memory<byte>(buffer, 0, blockReadResult.HeaderLength).Span,
                secureHeaderChars);

            var secureHeader = new RequestHeader(secureHeaderChars, true, _http11Parser);

            var bodyStream = sslStream;

            if (blockReadResult.TotalReadLength > blockReadResult.HeaderLength)
            {
                sslStream = new CombinedReadonlyStream(false,
                    new MemoryStream(buffer, blockReadResult.HeaderLength,
                        blockReadResult.TotalReadLength - blockReadResult.HeaderLength),
                    sslStream); 
            }

            return new Exchange(authority, secureHeader,
                secureHeader.ContentLength > 0
                    ? new ContentBoundStream(sslStream, secureHeader.ContentLength)
                    : StreamUtils.EmptyStream, null
            );
        }
    }
}