// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Security.Authentication;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;

namespace Fluxzy.Bulk.BcCli
{
    class FluxzyTlsClient : DefaultTlsClient
    {
        private readonly SslProtocols _sslProtocols;

        public FluxzyTlsClient(TlsCrypto crypto, SslProtocols sslProtocols)
            : base(crypto)
        {
            _sslProtocols = sslProtocols;
        }

        public override TlsAuthentication GetAuthentication()
        {
            return new FluxzyTlsAuthentication(); 
        }

        protected override ProtocolVersion[] GetSupportedVersions()
        {
            // map ProtocolVersion with SsslProcols 

            var listProtocolVersion = new List<ProtocolVersion>();

            if (SslProtocols.None == _sslProtocols)
            {
                return base.GetSupportedVersions();
            }

            if (_sslProtocols.HasFlag(SslProtocols.Tls))
            {
                listProtocolVersion.Add(ProtocolVersion.TLSv10);
            }

            if (_sslProtocols.HasFlag(SslProtocols.Tls11))
            {
                listProtocolVersion.Add(ProtocolVersion.TLSv11);
            }

            if (_sslProtocols.HasFlag(SslProtocols.Tls12))
            {
                listProtocolVersion.Add(ProtocolVersion.TLSv12);
            }

            if (_sslProtocols.HasFlag(SslProtocols.Tls13))
            {
                listProtocolVersion.Add(ProtocolVersion.TLSv13);
            }

            return listProtocolVersion.ToArray();
        }
    }
}