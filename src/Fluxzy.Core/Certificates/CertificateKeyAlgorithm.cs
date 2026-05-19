// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Certificates
{
    /// <summary>
    ///  The asymmetric key algorithm used when building a certificate.
    /// </summary>
    public enum CertificateKeyAlgorithm
    {
        /// <summary>
        ///  RSA key. The key length is controlled by <see cref="CertificateBuilderOptions.KeySize"/>.
        /// </summary>
        Rsa = 0,

        /// <summary>
        ///  Elliptic curve (ECDSA) key on the NIST P-224 / secp224r1 curve.
        /// </summary>
        EcdsaP224,

        /// <summary>
        ///  Elliptic curve (ECDSA) key on the NIST P-256 / secp256r1 curve.
        /// </summary>
        EcdsaP256,

        /// <summary>
        ///  Elliptic curve (ECDSA) key on the NIST P-384 / secp384r1 curve.
        /// </summary>
        EcdsaP384,

        /// <summary>
        ///  Elliptic curve (ECDSA) key on the NIST P-521 / secp521r1 curve.
        /// </summary>
        EcdsaP521
    }
}
