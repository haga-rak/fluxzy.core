// Copyright © 2022 Haga Rakotoharivelo

using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Fluxzy.Clients
{
    public class ExchangeContext
    {
        public IAuthority Authority { get; }

        public ExchangeContext(IAuthority authority)
        {
            Authority = authority;
        }

        /// <summary>
        /// Host IP that shall be used to decrypt this trafic 
        /// </summary>
        public IPAddress RemoteHostIp { get; set; }

        /// <summary>
        /// Port of substitution 
        /// </summary>
        public int ? RemoteHostPort { get; set; }

        /// <summary>
        /// Client certificate for this exchange 
        /// </summary>
        public X509Certificate2Collection ClientCertificates { get; set; }

        /// <summary>
        /// true if fluxzy should not decrypt this exchange
        /// </summary>
        public bool BlindMode { get; set; }
    }

    
}