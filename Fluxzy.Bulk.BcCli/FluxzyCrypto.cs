// // Copyright 2022 - Haga Rakotoharivelo
// 

using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace Fluxzy.Bulk.BcCli
{
    class FluxzyCrypto : BcTlsCrypto
    {
        public FluxzyCrypto(SecureRandom sr)
            : base(sr)
        {
            
        }

        public override TlsSecret GenerateRsaPreMasterSecret(ProtocolVersion version)
        {
            var res =  base.GenerateRsaPreMasterSecret(version);
            return res; 
        }
        

        public override TlsSecret AdoptSecret(TlsSecret secret)
        {
            var resultSecret =  base.AdoptSecret(secret);

            // byte[] data = new byte[1024];
            var data = secret.Extract();
            MasterSecret = new byte[data.Length];
            data.CopyTo(MasterSecret, 0);
            
            return resultSecret; 
        }

        public byte[]?  MasterSecret { get; set; }
    }
}