using System;
using System.Threading.Tasks;

namespace Echoes.Core
{
    public interface ICertificateCache
    {
        byte[] Load(string baseCertificatSerialNumber, string rootDomain, Func<string, byte[]> certificateGeneratoringProcess); 
    }
}