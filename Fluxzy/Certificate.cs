// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

namespace Fluxzy
{
    public class Certificate
    {

        private X509Certificate2? _cachedCertificate = null;

        [JsonInclude]
        public CertificateRetrieveMode RetrieveMode { get; set; } = CertificateRetrieveMode.FluxzyDefault;

        /// <summary>
        /// The certificate thumb print when location type is FromUserStoreSerialNumber
        /// </summary>
        [JsonInclude]
        public string? SerialNumber { get; set; }

        /// <summary>
        /// The certificate file when location type is FromPkcs12
        /// </summary>
        [JsonInclude]
        public string? Pkcs12File { get; set; }

        /// <summary>
        /// The certificate password when location typ is FromPkcs12. Null with no password was set. 
        /// </summary>
        [JsonInclude]
        public string? Pkcs12Password { get; set; }
        

        public static Certificate LoadFromUserStoreByThumbprint(string thumbPrint)
        {
            return new Certificate()
            {
                RetrieveMode = CertificateRetrieveMode.FromUserStoreSerialNumber,
                SerialNumber = thumbPrint
            };
        }

        public static Certificate LoadFromPkcs12(string pkcs12File, string pkcs12Password)
        {
            return new Certificate()
            {
                RetrieveMode = CertificateRetrieveMode.FromPkcs12,
                Pkcs12File = pkcs12File,
                Pkcs12Password = pkcs12Password
            };
        }

        public static Certificate UseDefault()
        {
            return new Certificate()
            {
                RetrieveMode = CertificateRetrieveMode.FluxzyDefault
            };
        }
        
        public X509Certificate2 GetCertificate()
        {

            if (_cachedCertificate != null)
                return _cachedCertificate;

            switch (RetrieveMode)
            {
                case CertificateRetrieveMode.FluxzyDefault:
                    return _cachedCertificate = new X509Certificate2(FileStore.Fluxzy, "echoes");

                case CertificateRetrieveMode.FromUserStoreSerialNumber:
                {
                    using var store = new X509Store(StoreName.My,
                        StoreLocation.CurrentUser);

                    store.Open(OpenFlags.ReadOnly);

                    var certificate = store.Certificates.Find(X509FindType.FindBySerialNumber,
                            SerialNumber, false)
                        .OfType<X509Certificate2>()
                        .FirstOrDefault();

                    if (certificate == null)
                        throw new FluxzyException($"Could not retrieve certificate with serialNumber `{SerialNumber}`.");

                    if (!certificate.HasPrivateKey)
                    {
                        throw new FluxzyException($"Either certificate with thumbprint `{SerialNumber}` does not contains private key " +
                                                  $"or current user does not have enough rights to read.");

                    }

                    return _cachedCertificate = certificate;
                }
                case CertificateRetrieveMode.FromPkcs12:
                    return _cachedCertificate = new X509Certificate2(Pkcs12File, Pkcs12Password);

                default:
                    throw new ArgumentOutOfRangeException($"Unknown retrieve mode : {RetrieveMode}");
            }
        }

        protected bool Equals(Certificate other)
        {
            return RetrieveMode == other.RetrieveMode
                   && SerialNumber == other.SerialNumber
                   && Pkcs12File == other.Pkcs12File
                   && Pkcs12Password == other.Pkcs12Password;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((Certificate)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)RetrieveMode, SerialNumber, Pkcs12File, Pkcs12Password);
        }
    }
}