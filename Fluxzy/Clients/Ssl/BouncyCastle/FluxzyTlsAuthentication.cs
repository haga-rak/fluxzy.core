using Org.BouncyCastle.Tls;

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    class FluxzyTlsAuthentication : TlsAuthentication
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