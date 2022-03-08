// Copyright © 2022 Haga Rakotoharivelo

using System.Security.Cryptography.X509Certificates;

namespace Echoes;

public class CertificateConfiguration
{
    public CertificateConfiguration(byte[] rawCertificate, string password)
    {
        if (rawCertificate == null)
            return; 

        Certificate = new X509Certificate2(rawCertificate, password);
    }


    internal X509Certificate2 Certificate { get; set; }


    public bool DefaultConfig
    {
        get
        {
            return Certificate == null;
        }
    }
}