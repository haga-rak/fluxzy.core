// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Org.BouncyCastle.Tls.Crypto;

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    internal static class SecretKeyExtractor
    {
        private static readonly EncryptInPlain Encryptor = new();

        public static byte[] ExtractKeySilently(this TlsSecret secret)
        {
            return secret.Encrypt(Encryptor);
        }
    }
}
