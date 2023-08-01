// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;
using CertificateRequest = Org.BouncyCastle.Tls.CertificateRequest;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    internal class FluxzyTlsAuthentication : TlsAuthentication
    {
        private readonly BcTlsCrypto _crypto;
        private readonly X509Certificate2? _certificate;

        public FluxzyTlsAuthentication(BcTlsCrypto crypto, X509Certificate2 ? certificate)
        {
            _crypto = crypto;
            _certificate = certificate;
        }

        public void NotifyServerCertificate(TlsServerCertificate serverCertificate)
        {

        }

        public TlsCredentials? GetClientCredentials(CertificateRequest certificateRequest)
        {
            if (_certificate == null)
                return null;

           // var rawP12 = _certificate.Export(X509ContentType.Pkcs12, "a");


            X509Certificate tempCert = DotNetUtilities.FromX509Certificate(_certificate);
  

            var tlsCertificate = new BcTlsCertificate(_crypto, tempCert.CertificateStructure);

            var c = new Certificate(new TlsCertificate[] { tlsCertificate });
            
            return new BcDefaultTlsCredentialedAgreement(_crypto, c,
                DotNetUtilities.GetKeyPair(_certificate.PrivateKey).Private);
        }
    }

    internal class CredentialsWithDotnetCert : TlsCredentials
    {
        public CredentialsWithDotnetCert(X509Certificate2 certificate, BcTlsCrypto crypto)
        {
            X509Certificate tempCert = DotNetUtilities.FromX509Certificate(certificate);
            var tlsCertificate = new BcTlsCertificate(crypto, tempCert.CertificateStructure);
            Certificate = new Certificate(new TlsCertificate[] { tlsCertificate });
        }


        public Certificate Certificate { get; }
    }
}
