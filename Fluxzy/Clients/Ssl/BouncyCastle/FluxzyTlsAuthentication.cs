using Org.BouncyCastle.Tls;

namespace Fluxzy.Bulk.BcCli;

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