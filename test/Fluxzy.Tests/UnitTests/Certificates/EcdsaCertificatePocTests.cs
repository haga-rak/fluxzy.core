// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fluxzy.Certificates;
using Fluxzy.Core;
using Fluxzy.Misc.IpUtils;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Certificates
{
    /// <summary>
    ///     Proof-of-concept coverage for elliptic curve (ECDSA) certificate support, relying solely
    ///     on the .NET 10 <c>System.Security.Cryptography</c> primitives.
    ///
    ///     Two entry points are exercised:
    ///      - <see cref="CertificateBuilder" /> creating an ECDSA root CA from scratch (P-224/256/384/521).
    ///      - <see cref="CertificateProvider" /> deriving leaf certificates from an ECDSA root CA, both
    ///        from roots built in-process and from roots embedded as test fixtures
    ///        (<c>_Files/Certificates/ecdsa-*-root-ca.p12</c>, generated with openssl).
    /// </summary>
    public class EcdsaCertificatePocTests : ProduceDeletableItem
    {
        // OID prefix shared by every ecdsa-with-SHA* signature algorithm (1.2.840.10045.4.x).
        private const string EcdsaSignatureOidPrefix = "1.2.840.10045.4";

        private const string EmbeddedP256Root = "_Files/Certificates/ecdsa-p256-root-ca.p12";
        private const string EmbeddedP224Root = "_Files/Certificates/ecdsa-p224-root-ca.p12";

        /// <summary>
        ///     CertificateBuilder can produce a self-signed ECDSA root CA on each supported NIST curve.
        /// </summary>
        [Theory]
        [InlineData(CertificateKeyAlgorithm.EcdsaP224, 224)]
        [InlineData(CertificateKeyAlgorithm.EcdsaP256, 256)]
        [InlineData(CertificateKeyAlgorithm.EcdsaP384, 384)]
        [InlineData(CertificateKeyAlgorithm.EcdsaP521, 521)]
        public void CertificateBuilder_CreatesEcdsaRootCa(CertificateKeyAlgorithm algorithm, int expectedKeySize)
        {
            // Arrange
            var options = new CertificateBuilderOptions($"Fluxzy ECC POC {algorithm}") {
                KeyAlgorithm = algorithm,
                Organization = "Fluxzy",
                P12Password = "poc"
            };

            // Act
            using var caCertificate = BuildAndLoadCa(options);

            // Assert : it is an ECDSA certificate authority on the requested curve
            using var ecdsaKey = caCertificate.GetECDsaPrivateKey();
            Assert.NotNull(ecdsaKey);
            Assert.Null(caCertificate.GetRSAPublicKey());
            Assert.Equal(expectedKeySize, ecdsaKey!.KeySize);
            Assert.True(caCertificate.HasPrivateKey);
            Assert.True(caCertificate.IsCa());
        }

        /// <summary>
        ///     The default key algorithm is unchanged: CertificateBuilder still yields an RSA root CA.
        /// </summary>
        [Fact]
        public void CertificateBuilder_StillCreatesRsaRootCa_ByDefault()
        {
            var options = new CertificateBuilderOptions("Fluxzy RSA POC") { P12Password = "poc" };

            using var caCertificate = BuildAndLoadCa(options);

            using var rsaKey = caCertificate.GetRSAPublicKey();
            Assert.NotNull(rsaKey);
            Assert.Null(caCertificate.GetECDsaPublicKey());
            Assert.True(caCertificate.IsCa());
        }

        /// <summary>
        ///     CertificateProvider derives a valid ECDSA leaf certificate from a pre-generated
        ///     ECDSA root CA embedded in the test project (openssl-generated fixture).
        /// </summary>
        [Theory]
        [InlineData(EmbeddedP256Root, 256)]
        [InlineData(EmbeddedP224Root, 256)] // P-224 root -> leaf clamped to P-256 (TLS 1.3 usable)
        public void CertificateProvider_DerivesEcdsaLeaf_FromEmbeddedRoot(string rootPath, int expectedLeafKeySize)
        {
            var rootCertificate = Certificate.LoadFromPkcs12(rootPath, "");

            AssertProviderProducesEcdsaLeaf(rootCertificate, expectedLeafKeySize);
        }

        /// <summary>
        ///     CertificateProvider derives a valid ECDSA leaf certificate from an ECDSA root CA
        ///     freshly created in-process by CertificateBuilder.
        /// </summary>
        [Theory]
        [InlineData(CertificateKeyAlgorithm.EcdsaP224, 256)] // clamped up to P-256
        [InlineData(CertificateKeyAlgorithm.EcdsaP256, 256)]
        [InlineData(CertificateKeyAlgorithm.EcdsaP384, 384)]
        [InlineData(CertificateKeyAlgorithm.EcdsaP521, 521)]
        public void CertificateProvider_DerivesEcdsaLeaf_FromBuilderCreatedRoot(
            CertificateKeyAlgorithm algorithm, int expectedLeafKeySize)
        {
            var options = new CertificateBuilderOptions($"Fluxzy ECC POC {algorithm}") {
                KeyAlgorithm = algorithm,
                P12Password = "poc"
            };

            var rootPath = GetRegisteredRandomFile();
            File.WriteAllBytes(rootPath, new CertificateBuilder(options).CreateSelfSigned());

            var rootCertificate = Certificate.LoadFromPkcs12(rootPath, "poc");

            AssertProviderProducesEcdsaLeaf(rootCertificate, expectedLeafKeySize);
        }

        /// <summary>
        ///     End-to-end proof: an ECDSA leaf certificate derived from an ECDSA root CA completes
        ///     a real TLS handshake when used as a server certificate.
        /// </summary>
        [Theory]
        [InlineData(EmbeddedP256Root)]
        [InlineData(EmbeddedP224Root)]
        public async Task EcdsaLeaf_CompletesTlsHandshake(string rootPath)
        {
            // Arrange
            var rootCertificate = Certificate.LoadFromPkcs12(rootPath, "");
            using var rootX509 = rootCertificate.GetX509Certificate();
            using var provider = new CertificateProvider(rootCertificate, new InMemoryCertificateCache());
            var serverCertificate = provider.GetCertificate("example.com");

            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint) listener.LocalEndpoint).Port;

            // Act : a full TLS handshake using the ECDSA leaf as the server certificate
            var serverTask = Task.Run(async () => {
                using var serverConnection = await listener.AcceptTcpClientAsync();
                using var serverSsl = new SslStream(serverConnection.GetStream(), false);
                await serverSsl.AuthenticateAsServerAsync(serverCertificate);
                return serverSsl.SslProtocol;
            });

            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);

            using var clientSsl = new SslStream(client.GetStream(), false,
                (_, certificate, _, _) => certificate is X509Certificate2 leaf && BuildsAgainst(leaf, rootX509));

            await clientSsl.AuthenticateAsClientAsync("example.com");
            var serverProtocol = await serverTask;

            // Assert
            Assert.True(clientSsl.IsAuthenticated);
            Assert.Contains(clientSsl.SslProtocol, new[] { SslProtocols.Tls12, SslProtocols.Tls13 });
            Assert.Equal(clientSsl.SslProtocol, serverProtocol);

            // The certificate presented by the server is the ECDSA leaf
            var remoteCertificate = clientSsl.RemoteCertificate as X509Certificate2;
            Assert.NotNull(remoteCertificate);

            using var remoteEcdsaKey = remoteCertificate!.GetECDsaPublicKey();
            Assert.NotNull(remoteEcdsaKey);

            listener.Stop();
        }

        private static void AssertProviderProducesEcdsaLeaf(Certificate rootCertificate, int expectedLeafKeySize)
        {
            // Arrange
            using var rootX509 = rootCertificate.GetX509Certificate();
            using var provider = new CertificateProvider(rootCertificate, new InMemoryCertificateCache());

            // Act
            var leaf = provider.GetCertificate("example.com");

            // Assert : the leaf is an ECDSA certificate...
            using var leafKey = leaf.GetECDsaPublicKey();
            Assert.NotNull(leafKey);
            Assert.Null(leaf.GetRSAPublicKey());

            // ...on the expected NIST curve...
            Assert.Equal(expectedLeafKeySize, leafKey!.KeySize);

            // ...signed by the root CA with an ECDSA signature...
            Assert.StartsWith(EcdsaSignatureOidPrefix, leaf.SignatureAlgorithm.Value);

            // ...carrying a usable private key...
            Assert.True(leaf.HasPrivateKey);

            // ...and it chains back to the ECDSA root CA.
            Assert.True(BuildsAgainst(leaf, rootX509), "Leaf certificate did not chain to the ECDSA root CA");
        }

        /// <summary>
        ///     Build a self-signed CA from <paramref name="options" /> and load it back as an
        ///     X509Certificate2. Routed through <see cref="Certificate.LoadFromPkcs12(string,string)" />
        ///     so the test relies on no .NET 9+ API and stays valid for the net8.0 target of Fluxzy.Core.
        /// </summary>
        private X509Certificate2 BuildAndLoadCa(CertificateBuilderOptions options)
        {
            var path = GetRegisteredRandomFile();
            File.WriteAllBytes(path, new CertificateBuilder(options).CreateSelfSigned());

            return Certificate.LoadFromPkcs12(path, options.P12Password!).GetX509Certificate();
        }

        /// <summary>
        ///     Build <paramref name="leaf" />'s chain trusting only <paramref name="root" />.
        /// </summary>
        private static bool BuildsAgainst(X509Certificate2 leaf, X509Certificate2 root)
        {
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(root);

            return chain.Build(leaf);
        }
    }
}
