// Copyright © 2022 Haga Rakotoharivelo

using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Echoes
{
    public class ClientSetting
    {
        public static ClientSetting Default { get; } = new ClientSetting(); 


        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostName"></param>
        /// <returns></returns>
        public X509Certificate2Collection GetCertificateByHost(string hostName)
        {
            return new X509Certificate2Collection();
        }

        /// <summary>
        /// Protocols supported by the current proxy 
        /// </summary>
        public SslProtocols ProxyTlsProtocols { get; set; } = SslProtocols.Tls12 | SslProtocols.Tls11;

        /// <summary>
        /// True to demand no decryption from proxy 
        /// </summary>
        public bool TunneledOnly { get; set; } = false;


        /// <summary>
        /// Process to validate the remote certificate 
        /// </summary>
        public RemoteCertificateValidationCallback CertificateValidationCallback { get; set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public int ConcurrentConnection { get; set; } = 8;


        public int TimeOutSecondsUnusedConnection { get; set; } = 20;


        /// <summary>
        /// Maximum header size 
        /// </summary>
        public int MaxHeaderSize { get; set; } = 16384; 

        /// <summary>
        /// Maximum header line
        /// </summary>
        public int MaxHeaderLineSize { get; set; } = 16384; 

    }
}