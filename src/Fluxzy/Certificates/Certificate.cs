// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using Fluxzy.Rules;

namespace Fluxzy.Certificates
{
    public class Certificate
    {
        private X509Certificate2? _cachedCertificate;

        [JsonConstructor]
        [Obsolete("This constructor is for serialization only (System.Text.Json limitation). Use static methods instead to avoid misconfiguration.")]
        public Certificate()
        {
            
        }

        [JsonInclude]
        [PropertyDistinctive(Description = "Retrieve mode")]
        public CertificateRetrieveMode RetrieveMode { get; set; } = CertificateRetrieveMode.FluxzyDefault;

        /// <summary>
        ///     The certificate serial number when location type is FromUserStoreSerialNumber
        /// </summary>
        [JsonInclude]
        [PropertyDistinctive(Description = "Serial number of a certificate available on user store")]
        public string? SerialNumber { get; set; }

        /// <summary>
        ///     The certificate thumb print when location type is FromUserStoreSerialNumber
        /// </summary>
        [JsonInclude]
        [PropertyDistinctive(Description = "Thumbprint of a certificate available on user store (hex format)")]
        public string? ThumbPrint { get; set; }

        /// <summary>
        ///     The certificate file when location type is FromPkcs12
        /// </summary>
        [JsonInclude]
        [PropertyDistinctive(Description = "Path to a PKCS#12 certificate")]
        public string? Pkcs12File { get; set; }

        /// <summary>
        ///     The certificate password when location typ is FromPkcs12. Null with no password was set.
        /// </summary>
        [JsonInclude]
        [PropertyDistinctive(Description = "Certificate passphrase when Pkcs12File is defined")]
        public string? Pkcs12Password { get; set; }

        public static Certificate LoadFromUserStoreByThumbprint(string thumbPrint)
        {
            return new Certificate {
                RetrieveMode = CertificateRetrieveMode.FromUserStoreThumbPrint,
                ThumbPrint = thumbPrint
            };
        }

        public static Certificate LoadFromUserStoreBySerialNumber(string serialNumber)
        {
            return new Certificate {
                RetrieveMode = CertificateRetrieveMode.FromUserStoreSerialNumber,
                SerialNumber = serialNumber
            };
        }

        public static Certificate LoadFromPkcs12(string pkcs12File, string pkcs12Password)
        {
            return new Certificate {
                RetrieveMode = CertificateRetrieveMode.FromPkcs12,
                Pkcs12File = pkcs12File,
                Pkcs12Password = pkcs12Password
            };
        }

        public static Certificate UseDefault()
        {
            return new Certificate {
                RetrieveMode = CertificateRetrieveMode.FluxzyDefault
            };
        }

        public X509Certificate2 GetX509Certificate()
        {
            if (_cachedCertificate != null)
                return _cachedCertificate;

            switch (RetrieveMode) {
                case CertificateRetrieveMode.FluxzyDefault:
                    return _cachedCertificate = new X509Certificate2(FileStore.Fluxzy, "echoes");

                case CertificateRetrieveMode.FromUserStoreSerialNumber: {
                    using var store = new X509Store(StoreName.My,
                        StoreLocation.CurrentUser);

                    store.Open(OpenFlags.ReadOnly);

                    var certificate = store.Certificates.Find(X509FindType.FindBySerialNumber,
                                               SerialNumber ?? throw new InvalidOperationException("SerialNumber is not set"), false)
                                           .OfType<X509Certificate2?>()
                                           .FirstOrDefault();

                    if (certificate == null)
                        throw new FluxzyException(
                            $"Could not retrieve certificate with serial number `{SerialNumber}`.");

                    if (!certificate.HasPrivateKey) {
                        throw new FluxzyException(
                            $"Either certificate with serial number `{SerialNumber}` does not contains private key " +
                            "or current user does not have enough rights to read.");
                    }

                    return _cachedCertificate = certificate;
                }

                case CertificateRetrieveMode.FromUserStoreThumbPrint: {
                    using var store = new X509Store(StoreName.My,
                        StoreLocation.CurrentUser);

                    store.Open(OpenFlags.ReadOnly);

                    var certificate = store.Certificates.Find(X509FindType.FindByThumbprint,
                                               ThumbPrint ?? throw new InvalidOperationException("ThumbPrint is not set"), false)
                                           .OfType<X509Certificate2?>()
                                           .FirstOrDefault();

                    if (certificate == null)
                        throw new FluxzyException($"Could not retrieve certificate with thumbPrint `{ThumbPrint}`.");

                    if (!certificate.HasPrivateKey) {
                        throw new FluxzyException(
                            $"Either certificate with thumbprint `{ThumbPrint}` does not contains private key " +
                            "or current user does not have enough rights to read.");
                    }

                    return _cachedCertificate = certificate;
                }

                case CertificateRetrieveMode.FromPkcs12:
                    return _cachedCertificate = new X509Certificate2(Pkcs12File ?? throw new InvalidOperationException("Pkcs12File is not set"), Pkcs12Password);

                default:
                    throw new ArgumentOutOfRangeException($"Unknown retrieve mode : {RetrieveMode}");
            }
        }

        protected bool Equals(Certificate other)
        {
            return RetrieveMode == other.RetrieveMode
                   && SerialNumber == other.SerialNumber
                   && ThumbPrint == other.ThumbPrint
                   && Pkcs12File == other.Pkcs12File
                   && Pkcs12Password == other.Pkcs12Password;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((Certificate) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int) RetrieveMode, SerialNumber, ThumbPrint, Pkcs12File, Pkcs12Password);
        }
    }
}
