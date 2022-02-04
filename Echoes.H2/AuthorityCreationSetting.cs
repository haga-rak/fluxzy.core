// Copyright © 2022 Haga Rakotoharivelo

using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Echoes.H2
{
    public class AuthorityCreationSetting
    {
        /// <summary>
        /// The used clientCertificate 
        /// </summary>
        public X509Certificate2Collection ClientCertificates { get; set; }

        /// <summary>
        /// Protocols supported by the current proxy 
        /// </summary>
        public SslProtocols ProxyTlsProtocols { get; set; }

        /// <summary>
        /// True to demand no decryption from proxy 
        /// </summary>
        public bool TunneledOnly { get; set; }


        /// <summary>
        /// Process to validate the remote certificate 
        /// </summary>
        public RemoteCertificateValidationCallback CertificateValidationCallback { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int ConcurrentConnection { get; set; } = 8;


        public int TimeOutSecondsUnusedConnection { get; set; } = 20;

    }
}