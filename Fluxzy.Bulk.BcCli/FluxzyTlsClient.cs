// // Copyright 2022 - Haga Rakotoharivelo
// 

using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;

namespace Fluxzy.Bulk.BcCli
{
    class FluxzyTlsClient : DefaultTlsClient
    {
        public FluxzyTlsClient(TlsCrypto crypto)
            : base(crypto)
        {
        }

        public override TlsAuthentication GetAuthentication()
        {
            return new FluxzyTlsAuthentication(); 
        }

        protected override ProtocolVersion[] GetSupportedVersions()
        {
            var versions =  base.GetSupportedVersions();

            return new[] {ProtocolVersion.TLSv11}; 

           // return versions; 
        }
    }
}