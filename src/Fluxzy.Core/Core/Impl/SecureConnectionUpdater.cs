// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Logging;
using Fluxzy.Misc.Streams;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fluxzy.Core
{
    internal class SecureConnectionUpdater
    {
        private static readonly List<SslApplicationProtocol> H11Protocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 };
        private static readonly List<SslApplicationProtocol> H11AndH2Protocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http2, SslApplicationProtocol.Http11 };
        

        private readonly ICertificateProvider _certificateProvider;
        private readonly bool _serveH2;
        private readonly bool _recoverHostNameFromSni;
        private readonly ILogger _logger;

        public SecureConnectionUpdater(
            ICertificateProvider certificateProvider, bool serveH2,
            bool recoverHostNameFromSni = false, ILogger? logger = null)
        {
            _certificateProvider = certificateProvider;
            _serveH2 = serveH2;
            _recoverHostNameFromSni = recoverHostNameFromSni;
            _logger = logger ?? NullLogger.Instance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DetectTlsClientHello(ReadOnlySpan<byte> data)
        {
            return data[0] == 0x16;
        }

        public async Task<SecureConnectionUpdateResult> AuthenticateAsServer(
            Stream stream, string host, ExchangeContext context, CancellationToken token)
        {
            var buffer = new byte[1];
            var originalStream = stream;

            if (stream is NetworkStream networkStream && networkStream.DataAvailable) {
                networkStream.ReadExact(buffer);
            }
            else {
                await stream.ReadExactAsync(buffer, token).ConfigureAwait(false);
            }

            if (!DetectTlsClientHello(buffer)) {
                // This is a regular CONNECT request without SSL
                return new SecureConnectionUpdateResult(false, new CombinedReadonlyStream(false, buffer, stream), stream);
            }

            stream = new CombinedReadonlyStream(false, buffer, stream);

            var secureStream = new SslStream(new RecomposedStream(stream, originalStream), false);

            // When recovery is enabled and the connect authority is an IP literal (full-system /
            // SOCKS5 capture where the client connected straight to a resolved IP), name the leaf
            // from the TLS SNI instead. Resolution then happens in the selection callback, where the
            // SNI is known. The default path keeps eager resolution, so behavior and cost are unchanged.
            var recoverFromSni = _recoverHostNameFromSni && IPAddress.TryParse(host, out _);

            X509Certificate2? prebuiltCertificate = null;

            if (!recoverFromSni) {
                try {
                    prebuiltCertificate = context.ServerCertificate ?? _certificateProvider.GetCertificate(host);
                }
                catch (Exception e) {
                    FluxzyLogEvents.CertificateResolutionFailed(_logger, e, host);
                    throw;
                }
            }

            string? observedSniHost = null;

            try {

                var effectiveServeH2 = _serveH2 && !context.ForceServeHttp11;

                var sslProtocols = SslProtocols.None;

                if (effectiveServeH2) {
                    sslProtocols = SslProtocols.Tls12;

#if NET8_0_OR_GREATER
                    sslProtocols |= SslProtocols.Tls13;
#endif
                }

                var sslServerAuthenticationOptions = new SslServerAuthenticationOptions
                {
                    ApplicationProtocols = effectiveServeH2 ? H11AndH2Protocols : H11Protocols,
                    EnabledSslProtocols = sslProtocols,
                    ClientCertificateRequired = false,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    EncryptionPolicy = EncryptionPolicy.RequireEncryption,
                    ServerCertificateSelectionCallback = (sender, name) =>
                    {
                        if (context.ServerCertificate is not null)
                            return context.ServerCertificate;

                        if (!recoverFromSni)
                            return prebuiltCertificate!;

                        // Prefer a usable SNI hostname; ignore empty SNI and IP-literal SNI and keep
                        // the IP fallback (current behavior) in those cases.
                        var sniUsable = !string.IsNullOrWhiteSpace(name) && !IPAddress.TryParse(name, out _);
                        var certHost = sniUsable ? name! : host;

                        if (sniUsable)
                            observedSniHost = name;

                        try {
                            return _certificateProvider.GetCertificate(certHost);
                        }
                        catch (Exception e) {
                            FluxzyLogEvents.CertificateResolutionFailed(_logger, e, certHost);
                            throw;
                        }
                    }
                };

                await secureStream
                    .AuthenticateAsServerAsync(sslServerAuthenticationOptions, token)
                    .ConfigureAwait(false);
                
            }
            catch (Exception ex) {
                throw new FluxzyException(
                    $"Impersonating “{host}” failed: {ex.Message}", ex)
                    {
                        TargetHost = host
                    };
            }

            return new SecureConnectionUpdateResult(true, secureStream, secureStream,
                secureStream.NegotiatedApplicationProtocol, observedSniHost);
        }
    }

    internal record SecureConnectionUpdateResult(
        bool IsSsl, Stream InStream, Stream OutStream,
        SslApplicationProtocol NegotiatedApplicationProtocol = default,
        string? SniHost = null)
    {
        public bool IsSsl { get; } = IsSsl;

        public Stream InStream { get; } = InStream;

        public Stream OutStream { get; } = OutStream;

        public SslApplicationProtocol NegotiatedApplicationProtocol { get; } = NegotiatedApplicationProtocol;

        /// <summary>
        ///     The hostname observed in the TLS SNI when leaf-from-SNI recovery applied (i.e. the
        ///     connect authority was an IP and the client sent a usable SNI). Null otherwise.
        /// </summary>
        public string? SniHost { get; } = SniHost;
    }
}
