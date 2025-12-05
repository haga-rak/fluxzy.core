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

namespace Fluxzy.Core
{
    internal class ReverseProxyExchangeSourceProvider : ExchangeSourceProvider
    {
        private readonly ICertificateProvider _certificateProvider;
        private readonly IIdProvider _idProvider;
        private readonly int? _reverseModeForcedPort;
        private readonly IExchangeContextBuilder _contextBuilder;

        private static readonly string TransparentExchangeConnectHeaderTemplate = 
            $"CONNECT {{0}}:{{1}} HTTP/1.1\r\n" +
            $"Host: {{0}}:{{1}}\r\n" +
            $"\r\n";

        public ReverseProxyExchangeSourceProvider(ICertificateProvider certificateProvider, IIdProvider idProvider,
            int ? reverseModeForcedPort, IExchangeContextBuilder contextBuilder)
                : base(idProvider)
        {
            _certificateProvider = certificateProvider;
            _idProvider = idProvider;
            _reverseModeForcedPort = reverseModeForcedPort;
            _contextBuilder = contextBuilder;
        }

        public override async ValueTask<ExchangeSourceInitResult?> InitClientConnection(
            Stream stream,
            RsBuffer buffer,
            IPEndPoint localEndpoint, IPEndPoint remoteEndPoint,
            CancellationToken token)
        {
            var secureStream = new SslStream(stream, false);

            var authorityName =  (string?) null;

            var sslServerAuthenticationOptions = new SslServerAuthenticationOptions {
                ApplicationProtocols = new() { SslApplicationProtocol.Http11 },
                ClientCertificateRequired = false,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                EncryptionPolicy = EncryptionPolicy.RequireEncryption,
                ServerCertificateSelectionCallback = (_, name) => {
                    var certificate = _certificateProvider.GetCertificate(string.IsNullOrWhiteSpace(name) ? "fluxzy.io" : name);
                    authorityName = name;
                    return certificate;
                }
            };

            await secureStream.AuthenticateAsServerAsync(sslServerAuthenticationOptions, token).ConfigureAwait(false);

            if (authorityName is null) {
                throw new FluxzyException("Unable to gather remote authority hostname");
            }

            var destinationPort = _reverseModeForcedPort ?? localEndpoint.Port; 

            var authority = new Authority(authorityName, destinationPort, true);

            var exchangeContext = await _contextBuilder.Create(authority, true).ConfigureAwait(false);

            var receivedFromProxy = ITimingProvider.Default.Instant();
            var certStart = receivedFromProxy;
            var certEnd = receivedFromProxy;

            var formattedHeaders = string.Format(TransparentExchangeConnectHeaderTemplate, 
                authorityName, destinationPort);
            
            var exchange = Exchange.CreateUntrackedExchange(_idProvider, exchangeContext,
                authority, formattedHeaders.AsMemory(), Stream.Null,
                ProxyConstants.AcceptTunnelResponseString.AsMemory(),
                Stream.Null, false, "HTTP/1.1", receivedFromProxy);

            exchange.Metrics.CreateCertStart = certStart;
            exchange.Metrics.CreateCertEnd = certEnd;

            var downStreamPipe = new Http11DownStreamPipe(_idProvider, authority, secureStream, secureStream, _contextBuilder);

            return new ExchangeSourceInitResult(downStreamPipe, exchange);
        }
    }
}
