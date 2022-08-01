using System;

namespace Fluxzy.Core
{
    public interface ICertificateCache
    {
        byte[] Load(string baseCertificatSerialNumber, string rootDomain, Func<string, byte[]> certificateGeneratoringProcess); 
    }
}