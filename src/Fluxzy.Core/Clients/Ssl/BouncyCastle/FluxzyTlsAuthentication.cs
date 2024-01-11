// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    internal class FluxzyTlsAuthentication : TlsAuthentication
    {
        private readonly FluxzyCrypto _tlsCrypto;
        private readonly BouncyCastleClientCertificateInfo? _clientCertificateInfo;

        public FluxzyTlsAuthentication(
            FluxzyCrypto tlsCrypto, 
            BouncyCastleClientCertificateInfo? clientCertificateInfo)
        {
            _tlsCrypto = tlsCrypto;
            _clientCertificateInfo = clientCertificateInfo;
        }

        public void NotifyServerCertificate(TlsServerCertificate serverCertificate)
        {

        }

        public TlsCredentials? GetClientCredentials(CertificateRequest certificateRequest)
        {
            if (_clientCertificateInfo != null) {
                var context = certificateRequest.GetCertificateRequestContext();

                var signatureAndHashAlgorithm = TlsUtilities.ChooseSignatureAndHashAlgorithm(_tlsCrypto.Context,
                    certificateRequest.SupportedSignatureAlgorithms, SignatureAlgorithm.rsa);

                var config = BouncyCastleClientCertificateConfiguration.CreateFrom(certificateRequest, _tlsCrypto,
                    _clientCertificateInfo);

                var cryptoParameters = new TlsCryptoParameters(_tlsCrypto.Context); 

                var credentials = new BcDefaultTlsCredentialedSigner(
                    cryptoParameters,
                    _tlsCrypto, config.PrivateKey, config.Certificate,
                    signatureAndHashAlgorithm);

                return credentials;
            }

            return null;
        }
    }
}
