// // Copyright 2022 - Haga Rakotoharivelo
// 

using Org.BouncyCastle.Security;
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
        
        public override TlsSecret AdoptSecret(TlsSecret secret)
        {
            var resultSecret =  base.AdoptSecret(secret);

            MasterSecret  = secret.ExtractKeySilently();
            
            //_writter.Write(NssLogWriter.CLIENT_RANDOM, PlainSecurityParameters.ClientRandom,
            //    PlainSecurityParameters.TrafficSecretClient.ExtractKeySilently());

            return resultSecret; 
        }

        public byte[]?  MasterSecret { get; set; }
    }
}