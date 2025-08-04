// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using Fluxzy.Rules;

namespace Fluxzy.Certificates
{
    /// <summary>
    /// Holds information about an X509 certificate configuration
    /// </summary>
    public class Certificate
    {
        private X509Certificate2? _cachedCertificate;

        [JsonConstructor]
        [Obsolete("This constructor is for serialization only (System.Text.Json limitation). Use static methods instead to avoid misconfiguration.")]
        public Certificate()
        {
            
        }

        private Certificate(CertificateRetrieveMode retrieveMode)
        {
            RetrieveMode = retrieveMode;
        }

        /// <summary>
        /// Defines how to retrieve the certificate
        /// </summary>
        [JsonInclude]
        [PropertyDistinctive(Description = "Retrieve mode" , DefaultValue = "fluxzyDefault")]
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

        /// <summary>
        /// Create a new instance from a certificate in the current user store by its thumbprint
        /// </summary>
        /// <param name="thumbPrint"></param>
        /// <returns></returns>
        public static Certificate LoadFromUserStoreByThumbprint(string thumbPrint)
        {
            return new Certificate(CertificateRetrieveMode.FromUserStoreThumbPrint) {
                ThumbPrint = thumbPrint
            };
        }

        /// <summary>
        ///    Create a new instance from a certificate in the current user store by its serialNumber
        /// </summary>
        /// <param name="serialNumber"></param>
        /// <returns></returns>
        public static Certificate LoadFromUserStoreBySerialNumber(string serialNumber)
        {
            return new Certificate(CertificateRetrieveMode.FromUserStoreSerialNumber) {
                SerialNumber = serialNumber
            };
        }

        /// <summary>
        ///   Create a new instance from a PCKS12 file with a password
        /// </summary>
        /// <param name="pkcs12File"></param>
        /// <param name="pkcs12Password"></param>
        /// <returns></returns>
        public static Certificate LoadFromPkcs12(string pkcs12File, string pkcs12Password)
        {
            return new Certificate(CertificateRetrieveMode.FromPkcs12) {
                Pkcs12File = pkcs12File,
                Pkcs12Password = pkcs12Password
            };
        }

        /// <summary>
        ///    Create a new instance from a PCKS12 file
        /// </summary>
        /// <param name="pkcs12File"></param>
        /// <returns></returns>
        public static Certificate LoadFromPkcs12(string pkcs12File)
        {
            return new Certificate(CertificateRetrieveMode.FromPkcs12) {
                Pkcs12File = pkcs12File,
            };
        }

        /// <summary>
        ///   Get the default built-in fluxzy certificate
        /// </summary>
        /// <returns></returns>
        public static Certificate UseDefault()
        {
            return new Certificate(CertificateRetrieveMode.FluxzyDefault);
        }

        /// <summary>
        /// Retrieve the current certificate as X509Certificate2
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="FluxzyException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public X509Certificate2 GetX509Certificate()
        {
            if (_cachedCertificate != null)
                return _cachedCertificate;

            switch (RetrieveMode) {
                case CertificateRetrieveMode.FluxzyDefault:
                    return _cachedCertificate = FluxzySecurityParams.Current.BuiltinCertificate;

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
                    return _cachedCertificate = (Pkcs12Password != null ? 
                        new X509Certificate2(Pkcs12File ?? throw new InvalidOperationException("Pkcs12File is not set"), Pkcs12Password,
                            X509KeyStorageFlags.MachineKeySet |
                            X509KeyStorageFlags.PersistKeySet |
                            X509KeyStorageFlags.Exportable) :
                        new X509Certificate2(Pkcs12File ?? throw new InvalidOperationException("Pkcs12File is not set"))) ;
                        

                default:
                    throw new ArgumentOutOfRangeException($"Unknown retrieve mode : {RetrieveMode}");
            }
        }

        /// <summary>
        ///  Check if the certificate is equal to another certificate. Don't use this method to check if the certificate is the same
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        protected bool Equals(Certificate other)
        {
            return RetrieveMode == other.RetrieveMode
                   && SerialNumber == other.SerialNumber
                   && ThumbPrint == other.ThumbPrint
                   && Pkcs12File == other.Pkcs12File
                   && Pkcs12Password == other.Pkcs12Password;
        }

        /// <summary>
        ///  Check if the certificate is equal to another certificate. Don't use this method to check if the certificate is the same
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
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
