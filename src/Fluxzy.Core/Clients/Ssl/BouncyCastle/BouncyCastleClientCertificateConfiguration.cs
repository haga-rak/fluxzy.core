// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Linq;
using Fluxzy.Core;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    internal class BouncyCastleClientCertificateConfiguration
    {
        public BouncyCastleClientCertificateConfiguration(Certificate certificate, AsymmetricKeyParameter privateKey)
        {
            Certificate = certificate;
            PrivateKey = privateKey;
        }

        public Certificate Certificate { get; }

        public AsymmetricKeyParameter PrivateKey { get; }

        public static BouncyCastleClientCertificateConfiguration CreateFrom(
            CertificateRequest certificateRequest,
            FluxzyCrypto fluxzyCrypto, BouncyCastleClientCertificateInfo info)
        {
            var store = new Pkcs12StoreBuilder().Build();

            using var stream = File.OpenRead(info.Pkcs12File);

            store.Load(stream, info.Pkcs12Password?.ToCharArray() ?? Array.Empty<char>());

            var mainCert = store.Aliases.First();

            var certificateEntry = store.GetCertificate(mainCert);

            var x509Certificate = certificateEntry.Certificate;

            var tlsCertificate = new BcTlsCertificate(fluxzyCrypto, x509Certificate.CertificateStructure);

            var certificate = new Certificate(
                // certificateRequest.GetCertificateRequestContext(),
                new[] { tlsCertificate });


            var keyEntry = store.GetKey(mainCert);

            var rsaKeyParameters = (RsaKeyParameters) keyEntry.Key;

            return new BouncyCastleClientCertificateConfiguration(certificate, rsaKeyParameters);
        }
    }

    public class BouncyCastleClientCertificateInfo
    {
        public BouncyCastleClientCertificateInfo(string pkcs12File, string? pkcs12Password = null)
        {
            Pkcs12File = pkcs12File;
            Pkcs12Password = pkcs12Password;
        }

        public string Pkcs12File { get;  }

        public string? Pkcs12Password{ get;  }
    }
}
