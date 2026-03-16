// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Fluxzy.Tests._Fixtures;

namespace Fluxzy.Tests.Cli.Scaffolding
{
    public class ProxiedHttpClient : IDisposable
    {
        private readonly HttpMessageHandler _handler;

        public ProxiedHttpClient(
            int port, string remoteHost = "127.0.0.1",
            bool allowAutoRedirect = true, CookieContainer? cookieContainer = null,
            bool automaticDecompression = false, NetworkCredential  ? proxyCredential = null,
            bool useSock5 = false, bool socks5WithH2 = false)
        {
            if (socks5WithH2) {
                // Use SocketsHttpHandler with SOCKS5 + H2 ALPN negotiation
                var proxyEndPoint = new IPEndPoint(IPAddress.Parse(remoteHost), port);
                _handler = CreateSocks5H2Handler(proxyEndPoint, allowAutoRedirect, cookieContainer, automaticDecompression);
                Client = new HttpClient(_handler) {
                    DefaultRequestVersion = new Version(2, 0),
                    DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
                };
            }
            else {
                WebProxy = proxyCredential == null
                        ? new WebProxy($"http://{remoteHost}:{port}")
                        : new WebProxy($"http://{remoteHost}:{port}", false, null, proxyCredential)
                    ;

                if (useSock5) {

                    WebProxy = proxyCredential == null
                        ? new WebProxy($"socks5://{remoteHost}:{port}")
                        : new WebProxy($"socks5://{remoteHost}:{port}", false, null, proxyCredential);
                }

                var clientHandler = new HttpClientHandler {
                    Proxy = WebProxy,
                    UseProxy = true,
                    AllowAutoRedirect = allowAutoRedirect,
                    ServerCertificateCustomValidationCallback = (_, certificate2, arg3, _) => {
                        ServerCertificate = certificate2;
                        ServerChain = arg3;
                        ServerCertificateIssuer = certificate2?.Issuer;
                        return true;
                    },
                };

                if (cookieContainer != null)
                    clientHandler.CookieContainer = cookieContainer;

                if (automaticDecompression) {
                    clientHandler.AutomaticDecompression = DecompressionMethods.GZip
                                             | DecompressionMethods.Deflate | DecompressionMethods.Brotli;
                }

                _handler = clientHandler;
                Client = new HttpClient(_handler);
            }
        }

        private static SocketsHttpHandler CreateSocks5H2Handler(
            IPEndPoint proxyEndPoint, bool allowAutoRedirect,
            CookieContainer? cookieContainer, bool automaticDecompression)
        {
            var normalized = proxyEndPoint;

            if (proxyEndPoint.Address.Equals(IPAddress.Any))
                normalized = new IPEndPoint(IPAddress.Loopback, proxyEndPoint.Port);

            var handler = new SocketsHttpHandler {
                ConnectCallback = async (context, cancellationToken) => {
                    var socket = new Socket(normalized.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    await socket.ConnectAsync(normalized, cancellationToken);

                    var stream = new NetworkStream(socket, ownsSocket: true);
                    await Socks5ClientFactory.PerformSocks5HandshakeAsync(stream, context.DnsEndPoint, cancellationToken);

                    return stream;
                },
                SslOptions = new SslClientAuthenticationOptions {
                    ApplicationProtocols = new List<SslApplicationProtocol> {
                        SslApplicationProtocol.Http2,
                        SslApplicationProtocol.Http11
                    },
                    RemoteCertificateValidationCallback = (_, _, _, _) => true
                },
                AllowAutoRedirect = allowAutoRedirect
            };

            if (cookieContainer != null)
                handler.CookieContainer = cookieContainer;

            if (automaticDecompression) {
                handler.AutomaticDecompression = DecompressionMethods.GZip
                                     | DecompressionMethods.Deflate | DecompressionMethods.Brotli;
            }

            return handler;
        }

        public WebProxy? WebProxy { get; set; }

        public X509Chain? ServerChain { get; private set; }

        public X509Certificate2?  ServerCertificate { get; private set; }

        public string ?  ServerCertificateIssuer { get; private set; }

        public HttpClient Client { get; }

        public void Dispose()
        {
            _handler.Dispose();
            Client.Dispose();
        }
    }
}
