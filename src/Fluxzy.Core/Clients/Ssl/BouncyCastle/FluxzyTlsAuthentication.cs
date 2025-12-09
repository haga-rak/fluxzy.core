// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Pkix;
using Org.BouncyCastle.Security.Certificates;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;
using Org.BouncyCastle.Utilities.Collections;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Store;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    internal class FluxzyTlsAuthentication : TlsAuthentication
    {
        private readonly FluxzyCrypto _tlsCrypto;
        private readonly BouncyCastleClientCertificateInfo? _clientCertificateInfo;
        private readonly string _targetHost;
        private readonly bool _skipRemoteCertificateValidation;

        public FluxzyTlsAuthentication(
            FluxzyCrypto tlsCrypto,
            BouncyCastleClientCertificateInfo? clientCertificateInfo,
            string targetHost, bool skipRemoteCertificateValidation)
        {
            _tlsCrypto = tlsCrypto;
            _clientCertificateInfo = clientCertificateInfo;
            _targetHost = targetHost;
            _skipRemoteCertificateValidation = skipRemoteCertificateValidation;
        }

        public void NotifyServerCertificate(TlsServerCertificate serverCertificate)
        {
            if (_skipRemoteCertificateValidation)
                return;

            // 1. Check that we received certificates
            var certChain = serverCertificate.Certificate;
            if (certChain == null || certChain.IsEmpty)
            {
                throw new TlsFatalAlert(AlertDescription.certificate_required);
            }

            // 2. Get the end-entity certificate (first in chain)
            var tlsCert = certChain.GetCertificateAt(0);
            var x509Cert = new X509CertificateParser().ReadCertificate(tlsCert.GetEncoded());

            // 3. Check validity period
            try
            {
                x509Cert.CheckValidity(DateTime.UtcNow);
            }
            catch (CertificateExpiredException)
            {
                throw new TlsFatalAlert(AlertDescription.certificate_expired) {};
            }
            catch (CertificateNotYetValidException)
            {
                throw new TlsFatalAlert(AlertDescription.certificate_unknown);
            }

            // 4. Verify hostname matches certificate
            if (!VerifyHostname(x509Cert, _targetHost))
            {
                throw new TlsFatalAlert(AlertDescription.bad_certificate);
            }
        }

        private bool VerifyHostname(X509Certificate cert, string hostname)
        {
            // Check Subject Alternative Names first
            var sanExtension = cert.GetSubjectAlternativeNames();

            if (sanExtension != null)
            {
                foreach (var san in sanExtension)
                {
                    var entry = san;
                    string dnsName = (string) entry[1];
                    if (MatchesHostname(dnsName, hostname))
                        return true;
                }
            }

            // Fall back to Common Name (CN) in subject
            var subject = cert.SubjectDN;
            var cn = subject.GetValueList(X509Name.CN).Cast<string>().FirstOrDefault();
            return cn != null && MatchesHostname(cn, hostname);
        }

        private bool MatchesHostname(string pattern, string hostname)
        {
            // Handle wildcard certificates (e.g., *.example.com)
            if (pattern.StartsWith("*."))
            {
                string suffix = pattern.Substring(1); // ".example.com"
                int firstDot = hostname.IndexOf('.');
                if (firstDot > 0)
                {
                    return hostname.Substring(firstDot)
                        .Equals(suffix, StringComparison.OrdinalIgnoreCase);
                }
                return false;
            }

            return pattern.Equals(hostname, StringComparison.OrdinalIgnoreCase);
        }


        public TlsCredentials? GetClientCredentials(CertificateRequest certificateRequest)
        {
            if (_clientCertificateInfo != null) {
               var config = BouncyCastleClientCertificateConfiguration.CreateFrom(
                   certificateRequest, _tlsCrypto,
                    _clientCertificateInfo);

               var clientCertificate = config.Certificate.GetCertificateAt(0);

               var clientCertificateSignature = certificateRequest
                                     .SupportedSignatureAlgorithms
                                     .Where(s => clientCertificate.SupportsSignatureAlgorithm(s.Signature))
                                     .Select(s => s.Signature)
                                     .OrderByDescending(r => r >= 4 && r < 10) // Prefer PSS first
                                     .FirstOrDefault();

                var signatureAndHashAlgorithm = TlsUtilities
                   .ChooseSignatureAndHashAlgorithm(_tlsCrypto.Context,
                       certificateRequest.SupportedSignatureAlgorithms,
                       clientCertificateSignature
                   );

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
