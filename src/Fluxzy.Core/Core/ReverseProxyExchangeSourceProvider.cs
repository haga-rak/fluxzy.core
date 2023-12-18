// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Misc.ResizableBuffers;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Core
{
    internal class ReverseProxyExchangeSourceProvider : ExchangeSourceProvider
    {
        private readonly ICertificateProvider _certificateProvider;
        private readonly IIdProvider _idProvider;

        private static readonly string TransparentExchangeConnectHeaderTemplate = 
            $"GET / HTTP/1.1\r\n" +
            $"Host: {{0}}:{{1}}\r\n" +
            $"Connection: keep-alive\r\n" +
            $"Keep-alive: timeout=5\r\n" +
            $"\r\n";

        public ReverseProxyExchangeSourceProvider(ICertificateProvider certificateProvider, IIdProvider idProvider)
                : base(idProvider)
        {
            _certificateProvider = certificateProvider;
            _idProvider = idProvider;
        }

        public override async ValueTask<ExchangeSourceInitResult?> InitClientConnection(
            Stream stream,
            RsBuffer buffer,
            IExchangeContextBuilder contextBuilder,
            IPEndPoint ipEndPoint,
            CancellationToken token)
        {
            var secureStream = new SslStream(stream, false);

            var authorityName =  (string?) null;

            var sslServerAuthenticationOptions = new SslServerAuthenticationOptions {
                ApplicationProtocols = new() { SslApplicationProtocol.Http11 },
                ClientCertificateRequired = false,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                EncryptionPolicy = EncryptionPolicy.RequireEncryption,
                ServerCertificateSelectionCallback = (sender, name) => {
                    var certificate = _certificateProvider.GetCertificate(name ?? "fluxzy.io");
                    authorityName = name;
                    return certificate;
                }
            };

            await secureStream.AuthenticateAsServerAsync(sslServerAuthenticationOptions, token);

            if (authorityName is null) {
                throw new FluxzyException("Unable to gather remote authority hostname");
            }
            
            var authority = new Authority(authorityName, ipEndPoint.Port, true);

            var exchangeContext = await contextBuilder.Create(authority, true);

            var receivedFromProxy = ITimingProvider.Default.Instant();
            var certStart = receivedFromProxy;
            var certEnd = receivedFromProxy;

            var formattedHeaders = string.Format(TransparentExchangeConnectHeaderTemplate, authorityName,
                ipEndPoint.Port);
            
            var exchange = Exchange.CreateUntrackedExchange(_idProvider, exchangeContext,
                authority, formattedHeaders.AsMemory(), StreamUtils.EmptyStream,
                ProxyConstants.AcceptTunnelResponseString.AsMemory(),
                StreamUtils.EmptyStream, false, "HTTP/1.1", receivedFromProxy);

            exchange.Metrics.CreateCertStart = certStart;
            exchange.Metrics.CreateCertEnd = certEnd;

            return new(authority, secureStream, secureStream, exchange, false);
        }

        public override ValueTask<Exchange?> ReadNextExchange(
            Stream inStream, Authority authority, RsBuffer buffer,
            IExchangeContextBuilder contextBuilder,
            CancellationToken token)
        {
            throw new System.NotImplementedException();
        }
    }
}
