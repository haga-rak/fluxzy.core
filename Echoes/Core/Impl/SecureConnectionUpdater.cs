using System;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Core.Utils;

namespace Echoes.Core
{
    internal class SecureConnectionUpdater : ISecureConnectionUpdater
    {
        private readonly ICertificateProvider _certificateProvider;

        public SecureConnectionUpdater(ICertificateProvider certificateProvider)
        {
            _certificateProvider = certificateProvider;
        }
        

        public async Task<SslStream> AuthenticateAsServer(Stream stream, string host)
        {
           var secureStream = new SslStream(stream, false);

            using (var certificate = await _certificateProvider.GetCertificate(host).ConfigureAwait(false))
            {
                try
                {
                    await secureStream
                        .AuthenticateAsServerAsync(certificate, false, SslProtocols.None, false)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new EchoesException("Client closed connection while trying to negotiate SSL/TLS settings", ex);
                }
            }
            
            return secureStream; 
        }
    }
}