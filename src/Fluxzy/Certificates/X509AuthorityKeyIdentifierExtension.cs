// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Fluxzy.Certificates
{
    public class X509AuthorityKeyIdentifierExtension : X509Extension
    {
        public X509AuthorityKeyIdentifierExtension(X509Certificate2 certificateAuthority, bool critical)
            : base(AuthorityKeyIdentifierOid, EncodeExtension(certificateAuthority), critical)
        {
        }

        private static Oid AuthorityKeyIdentifierOid => new("2.5.29.35");

        private static Oid SubjectKeyIdentifierOid => new("2.5.29.14");

        private static byte[] EncodeExtension(X509Certificate2 certificateAuthority)
        {
            var subjectKeyIdentifier = certificateAuthority
                                       .Extensions.GetExtensions()
                                       .FirstOrDefault(p =>
                                           p.Oid?.Value == SubjectKeyIdentifierOid.Value);

            if (subjectKeyIdentifier == null)
                throw new InvalidOperationException("SubjectKeyIdentifier not found");

            var rawData = subjectKeyIdentifier.RawData;
            var segment = new ArraySegment<byte>(rawData, 2, rawData.Length - 2);

            var authorityKeyIdentifier = new byte[segment.Count + 4];

            // KeyID of the AuthorityKeyIdentifier
            authorityKeyIdentifier[0] = 0x30;
            authorityKeyIdentifier[1] = 0x16;
            authorityKeyIdentifier[2] = 0x80;
            authorityKeyIdentifier[3] = 0x14;
            segment.CopyTo(authorityKeyIdentifier, 4);

            return authorityKeyIdentifier;
        }
    }

    public static class X509ExtensionsExtensions
    {
        public static IEnumerable<X509Extension> GetExtensions(this X509ExtensionCollection collection)
        {
            foreach (var item in collection) {
                yield return item;
            }
        }
    }
}
