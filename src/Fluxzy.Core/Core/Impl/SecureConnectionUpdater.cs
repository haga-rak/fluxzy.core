// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Fluxzy.Misc.Traces;

namespace Fluxzy.Core
{
    internal class SecureConnectionUpdater
    {
        private static readonly List<SslApplicationProtocol> H11Protocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 };
        private static readonly List<SslApplicationProtocol> H11AndH2Protocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 , SslApplicationProtocol.Http2 };
        

        private readonly ICertificateProvider _certificateProvider;
        private readonly bool _serveH2;

        public SecureConnectionUpdater(ICertificateProvider certificateProvider, bool serveH2)
        {
            _certificateProvider = certificateProvider;
            _serveH2 = serveH2;
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

            X509Certificate2 certificate;

            try {
                certificate = context.ServerCertificate ?? _certificateProvider.GetCertificate(host);
            }
            catch (Exception e) {
                if (D.EnableTracing) {
                    D.TraceException(e, "An error occured while getting certificate");
                }
                
                throw;
            }

            try {
                var sslServerAuthenticationOptions = new SslServerAuthenticationOptions
                {
                    ApplicationProtocols = _serveH2 ? H11AndH2Protocols : H11Protocols,
                    ClientCertificateRequired = false,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    EncryptionPolicy = EncryptionPolicy.RequireEncryption,
                    EnabledSslProtocols = SslProtocols.None,
                    ServerCertificateSelectionCallback = (sender, name) => certificate
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

            return new SecureConnectionUpdateResult(true, secureStream, secureStream);
        }
    }

    internal record SecureConnectionUpdateResult(bool IsSsl, Stream InStream, Stream OutStream)
    {
        public bool IsSsl { get; } = IsSsl;
        
        public Stream InStream { get; } = InStream;

        public Stream OutStream { get; } = OutStream;
    }
}
