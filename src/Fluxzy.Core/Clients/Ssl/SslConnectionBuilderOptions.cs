// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Fluxzy.Certificates;
using Fluxzy.Clients.Ssl.BouncyCastle;
using Certificate = Fluxzy.Certificates.Certificate;

namespace Fluxzy.Clients.Ssl
{
    public class SslConnectionBuilderOptions
    {
        private SslClientAuthenticationOptions? _authenticationOptions; 

        public SslConnectionBuilderOptions(string targetHost, 
            SslProtocols enabledSslProtocols, List<SslApplicationProtocol> applicationProtocols,
            RemoteCertificateValidationCallback? remoteCertificateValidationCallback,
            Certificate? clientCertificate, AdvancedTlsSettings? advancedTlsSettings)
        {
            TargetHost = targetHost;
            EnabledSslProtocols = enabledSslProtocols;
            ApplicationProtocols = applicationProtocols;
            RemoteCertificateValidationCallback = remoteCertificateValidationCallback;
            ClientCertificate = clientCertificate;
            AdvancedTlsSettings = advancedTlsSettings;
        }

        public string TargetHost { get; }

        public SslProtocols EnabledSslProtocols { get;  }

        public List<SslApplicationProtocol> ApplicationProtocols { get; }

        public RemoteCertificateValidationCallback? RemoteCertificateValidationCallback { get; } 

        public Certificate ? ClientCertificate { get; set; }

        public AdvancedTlsSettings? AdvancedTlsSettings { get; }

        public SslClientAuthenticationOptions GetSslClientAuthenticationOptions()
        {
            if (_authenticationOptions != null)
                return _authenticationOptions; 

            var result =  new SslClientAuthenticationOptions {
                EnabledSslProtocols = EnabledSslProtocols,
                ApplicationProtocols = ApplicationProtocols,
                TargetHost = TargetHost
            };

            if (RemoteCertificateValidationCallback != null)
                result.RemoteCertificateValidationCallback = RemoteCertificateValidationCallback;

            if (ClientCertificate != null) {
                result.ClientCertificates ??= new X509CertificateCollection();
                result.ClientCertificates.Add(ClientCertificate.GetX509Certificate());
            }

            return _authenticationOptions = result;
        }

        internal BouncyCastleClientCertificateInfo? GetBouncyCastleClientCertificateInfo()
        {
            if (ClientCertificate == null)
                return null;

            if (ClientCertificate.RetrieveMode != CertificateRetrieveMode.FromPkcs12)
                throw new FluxzyException($"CertificateRetrieveMode must be FromPkcs12 when using Bouncy Castle");

            if (string.IsNullOrWhiteSpace(ClientCertificate.Pkcs12File))
                throw new FluxzyException($"Pkcs12File must be set when using Bouncy Castle");

            return new BouncyCastleClientCertificateInfo(ClientCertificate.Pkcs12File, ClientCertificate.Pkcs12Password);
        }
    }
}
