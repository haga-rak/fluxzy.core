// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    internal class BouncyCastleConnectionBuilder : ISslConnectionBuilder
    {
        private static readonly object SslFileLocker = new();

        public async Task<SslConnection> AuthenticateAsClient(
            Stream innerStream,
            SslConnectionBuilderOptions builderOptions,
            Action<string> onKeyReceived,
            CancellationToken token)
        {
            var crypto = new FluxzyCrypto();

            var skipRemoteCertificateValidation = builderOptions.ContextSkipRemoteCertificateValidation;

            var tlsAuthentication = 
                new FluxzyTlsAuthentication(crypto, 
                    builderOptions.GetBouncyCastleClientCertificateInfo(),
                    builderOptions.TargetHost, skipRemoteCertificateValidation);


            var fingerPrintEnforcer = new FingerPrintTlsExtensionsEnforcer();

            var client = new FluxzyTlsClient(builderOptions, tlsAuthentication, crypto, fingerPrintEnforcer);

            var memoryStream = new MemoryStream();

            var nssWriter = new NssLogWriter(memoryStream) {
                KeyHandler = onKeyReceived
            };

            if (Environment.GetEnvironmentVariable("SSLKEYLOGFILE") is { } str) {
                nssWriter.KeyHandler = nss => {
                    onKeyReceived(nss);
                    lock (SslFileLocker) {
                        try
                        {
                            File.AppendAllText(str, nss);
                        }
                        catch {
                            // ignore ERROR. SSLKEYLOGFILE May be locked by other process
                        }
                    }
                };
            }

            var protocol = new FluxzyClientProtocol(innerStream, fingerPrintEnforcer, nssWriter);

            // BC's ConnectAsync does not take a token: closing the inner stream is the
            // only way to abort a pending handshake read.
            using var tokenRegistration = token.Register(s => ((Stream) s!).Close(), innerStream);

            try
            {
                await protocol.ConnectAsync(client).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                token.ThrowIfCancellationRequested();

                var networkErrorCode = MapTlsAlert(ex);

                throw new ClientErrorException(0,
                    $"Handshake with {builderOptions.TargetHost} has failed",
                    innerMessageException: ex.Message,
                    innerException: ex,
                    networkErrorCode: networkErrorCode);
            }

            var keyInfos =
                Encoding.UTF8.GetString(memoryStream.ToArray());

            // TODO : Keyinfos may be get updated during runtime, must be updated in SslConnection

            var networkStream = innerStream as NetworkStream;

            if (networkStream == null && innerStream is DisposeEventNotifierStream disposeEventNotifierStream) {
                networkStream = (NetworkStream) disposeEventNotifierStream.InnerStream;
            }

            var exportCertificate =
                builderOptions.AdvancedTlsSettings?.ExportCertificateInSslInfo ?? false;

            var connection =
                new SslConnection(protocol.Stream, new SslInfo(protocol, exportCertificate), protocol.GetApplicationProtocol(),
                    networkStream, innerStream as DisposeEventNotifierStream) {
                    NssKey = keyInfos
                };

            return connection;
        }

        internal static string MapTlsAlert(Exception ex)
        {
            // Walk the inner-exception chain looking for a TlsFatalAlert raised by
            // either the BC stack itself or our FluxzyTlsAuthentication validator.
            for (var current = ex; current != null; current = current.InnerException) {
                if (current is Org.BouncyCastle.Tls.TlsFatalAlert alert) {
                    return alert.AlertDescription switch {
                        Org.BouncyCastle.Tls.AlertDescription.certificate_expired
                            => NetworkErrorCodes.TlsCertExpired,
                        Org.BouncyCastle.Tls.AlertDescription.bad_certificate
                            => NetworkErrorCodes.TlsCertHostnameMismatch,
                        Org.BouncyCastle.Tls.AlertDescription.unknown_ca
                            => NetworkErrorCodes.TlsCertUntrusted,
                        Org.BouncyCastle.Tls.AlertDescription.certificate_unknown
                            => NetworkErrorCodes.TlsCertInvalid,
                        Org.BouncyCastle.Tls.AlertDescription.certificate_required
                            => NetworkErrorCodes.TlsCertInvalid,
                        Org.BouncyCastle.Tls.AlertDescription.certificate_revoked
                            => NetworkErrorCodes.TlsCertInvalid,
                        _ => NetworkErrorCodes.TlsHandshakeFailure
                    };
                }
            }

            return NetworkErrorCodes.TlsHandshakeFailure;
        }
    }
}
