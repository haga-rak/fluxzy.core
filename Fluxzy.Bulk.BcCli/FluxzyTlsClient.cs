// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Net.Security;
using System.Security.Authentication;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;

namespace Fluxzy.Bulk.BcCli
{
    class FluxzyTlsClient : DefaultTlsClient
    {
        private readonly SslProtocols _sslProtocols;
        private readonly SslApplicationProtocol[] _applicationProtocols;

        public FluxzyTlsClient(TlsCrypto crypto, SslProtocols sslProtocols, 
             SslApplicationProtocol [] applicationProtocols)
            : base(crypto)
        {
            _sslProtocols = sslProtocols;
            _applicationProtocols = applicationProtocols;
        }

        public override TlsAuthentication GetAuthentication()
        {
            return new FluxzyTlsAuthentication(); 
        }

        protected override IList<ProtocolName> GetProtocolNames()
        {
            var result = new List<ProtocolName>();

            if (!_applicationProtocols.Any()) {
                return base.GetProtocolNames();
            }

            foreach (var applicationProtocol in _applicationProtocols) {
                
                if (applicationProtocol.Protocol.Equals(SslApplicationProtocol.Http11.Protocol))
                    result.Add(ProtocolName.Http_1_1);
                
                if (applicationProtocol.Protocol.Equals(SslApplicationProtocol.Http2.Protocol))
                    result.Add(ProtocolName.Http_2_Tls);
            }
            
            return result; 
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