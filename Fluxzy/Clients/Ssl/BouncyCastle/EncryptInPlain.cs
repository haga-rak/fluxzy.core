// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Org.BouncyCastle.Tls.Crypto;

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    internal class EncryptInPlain : TlsEncryptor
    {
        public byte[] Encrypt(byte[] input, int inOff, int length)
        {
            var key = new byte[length];
            input.AsSpan().Slice(inOff, length).CopyTo(key);

            return key;
        }
    }

    internal static class SecretKeyExtractor
    {
        private static readonly EncryptInPlain Encryptor = new();

        public static byte[] ExtractKeySilently(this TlsSecret secret)
        {
            return secret.Encrypt(Encryptor);
        }
    }
}
