// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Org.BouncyCastle.Tls;

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    internal class FluxzyTlsAuthentication : TlsAuthentication
    {
        public void NotifyServerCertificate(TlsServerCertificate serverCertificate)
        {
        }

        public TlsCredentials? GetClientCredentials(CertificateRequest certificateRequest)
        {
            return null;
        }
    }
}
