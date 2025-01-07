// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    internal class FluxzyCrypto : BcTlsCrypto
    {
        public TlsClientContext? Context { get; private set; }

        public FluxzyCrypto() : base(new SecureRandom())
        {
        }

        public byte[]? MasterSecret { get; set; }


        public override TlsSecret AdoptSecret(TlsSecret secret)
        {
            var resultSecret = base.AdoptSecret(secret);

            MasterSecret = secret.ExtractKeySilently();

            return resultSecret;
        }

        public void UpdateContext(TlsClientContext context)
        {
            Context = context;
        }
    }
}
