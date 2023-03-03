// // Copyright 2022 - Haga Rakotoharivelo
// 

using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    internal class FluxzyCrypto : BcTlsCrypto
    {
        public FluxzyCrypto()
            : base(new SecureRandom())
        {
        }
        
        public override TlsSecret AdoptSecret(TlsSecret secret)
        {
            var resultSecret =  base.AdoptSecret(secret);

            MasterSecret  = secret.ExtractKeySilently();

            return resultSecret; 
        }

        public byte[]?  MasterSecret { get; set; }
    }
}