// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Core;

namespace Fluxzy.Clients.Ssl.SChannel
{
    public class SChannelConnectionBuilder : ISslConnectionBuilder
    {
        public async Task<SslConnection> AuthenticateAsClient(
            Stream innerStream,
            SslConnectionBuilderOptions builderOptions, Action<string> onKeyReceived, CancellationToken token)
        {
            var sslStream = new SslStream(innerStream, false);

            var sslOptions = builderOptions.GetSslClientAuthenticationOptions();

            // Install a sniffing validation callback so that, if AuthenticateAsClientAsync
            // throws, we can map the failure to a specific NetworkErrorCodes token. The
            // sniffer wraps any user-supplied callback (or the default policy) and only
            // observes — it does not change the accept/reject decision.
            var inner = sslOptions.RemoteCertificateValidationCallback;
            SslPolicyErrors capturedPolicyErrors = SslPolicyErrors.None;
            X509ChainStatusFlags capturedChainStatus = X509ChainStatusFlags.NoError;
            var captured = false;

            sslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) => {
                capturedPolicyErrors = errors;
                if (chain != null) {
                    foreach (var status in chain.ChainStatus) {
                        capturedChainStatus |= status.Status;
                    }
                }
                captured = true;
                return inner != null ? inner(sender, cert, chain, errors) : errors == SslPolicyErrors.None;
            };

            try {
                await sslStream.AuthenticateAsClientAsync(sslOptions, token).ConfigureAwait(false);
            }
            catch (AuthenticationException ex) {
                var networkErrorCode = captured
                    ? MapPolicyToNetworkErrorCode(capturedPolicyErrors, capturedChainStatus)
                    : NetworkErrorCodes.TlsHandshakeFailure;

                throw new ClientErrorException(0,
                    $"Handshake with {builderOptions.TargetHost} has failed",
                    innerMessageException: ex.Message,
                    innerException: ex,
                    networkErrorCode: networkErrorCode);
            }

            var exportCertificate =
                builderOptions.AdvancedTlsSettings?.ExportCertificateInSslInfo ?? false;

            var sslInfo = new SslInfo(sslStream, exportCertificate);

            return new SslConnection(sslStream, sslInfo, sslStream.NegotiatedApplicationProtocol);
        }

        internal static string MapPolicyToNetworkErrorCode(
            SslPolicyErrors policyErrors,
            X509ChainStatusFlags chainStatus)
        {
            if ((policyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0) {
                return NetworkErrorCodes.TlsCertHostnameMismatch;
            }

            if ((chainStatus & X509ChainStatusFlags.NotTimeValid) != 0
                || (chainStatus & X509ChainStatusFlags.CtlNotTimeValid) != 0) {
                return NetworkErrorCodes.TlsCertExpired;
            }

            if ((chainStatus & X509ChainStatusFlags.UntrustedRoot) != 0
                || (chainStatus & X509ChainStatusFlags.PartialChain) != 0
                || (chainStatus & X509ChainStatusFlags.NoIssuanceChainPolicy) != 0) {
                return NetworkErrorCodes.TlsCertUntrusted;
            }

            if ((policyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0
                || (policyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) != 0) {
                return NetworkErrorCodes.TlsCertInvalid;
            }

            return NetworkErrorCodes.TlsHandshakeFailure;
        }
    }
}
