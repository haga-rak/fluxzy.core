// Copyright © 2022 Haga Rakotoharivelo

using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Echoes.Clients
{
    internal class ProxyRuntimeSetting
    {
        private readonly ProxyStartupSetting _startupSetting;

        public static ProxyRuntimeSetting Default { get; } = new();

        private ProxyRuntimeSetting()
        {

        }

        public ProxyRuntimeSetting(ProxyStartupSetting startupSetting)
        {
            _startupSetting = startupSetting;
            ProxyTlsProtocols = startupSetting.ServerProtocols;
            ConcurrentConnection = startupSetting.ConnectionPerHost;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostName"></param>
        /// <returns></returns>
        public X509Certificate2Collection GetCertificateByHost(string hostName)
        {
            var result = new X509Certificate2Collection();

            if (_startupSetting != null &&
                _startupSetting.ClientCertificateConfiguration != null &&
                _startupSetting.ClientCertificateConfiguration.HasConfig
                )
            {
                var certificate  = _startupSetting.ClientCertificateConfiguration.GetCustomClientCertificate(
                    hostName);
                
                if (certificate != null)
                    result.Add(certificate);
            }

            return result;
        }

        /// <summary>
        /// Protocols supported by the current proxy 
        /// </summary>
        public SslProtocols ProxyTlsProtocols { get; set; } = SslProtocols.Tls12 | SslProtocols.Tls11;
        
        internal bool ShouldTunneled(string hostName)
        {
            if (_startupSetting == null)
                return false;

            if (_startupSetting.SkipSslDecryption)
                return true;

            if (_startupSetting.TunneledOnlyHosts == null)
                return false;

            // TODO : Regex contains may be slow in critical usage. Consider using
            // TODO : dictionary or updating implementation of WildCardContains()

            return _startupSetting.TunneledOnlyHosts.WildCardContains(hostName); 
        }

        /// <summary>
        /// Process to validate the remote certificate 
        /// </summary>
        public RemoteCertificateValidationCallback CertificateValidationCallback { get; set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public int ConcurrentConnection { get; set; } = 8;


        public int TimeOutSecondsUnusedConnection { get; set; } = 4;
    }
}