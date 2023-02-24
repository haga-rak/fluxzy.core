﻿// // Copyright 2022 - Haga Rakotoharivelo
// 

using Org.BouncyCastle.Tls.Crypto;

namespace Fluxzy.Bulk.BcCli
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
        private static readonly EncryptInPlain _encryptor = new(); 

        public static byte[] ExtractKeySilently(this TlsSecret secret)
        {
            return secret.Encrypt(_encryptor);
        }
    }
}