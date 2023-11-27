// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Text;

namespace Fluxzy.Certificates
{
    /// <summary>
    ///  Certificate builder options
    /// </summary>
    public class CertificateBuilderOptions
    {
        public CertificateBuilderOptions(string commonName)
        {
            CommonName = commonName;
        }

        /// <summary>
        /// The common name of the certificate
        /// </summary>
        public string CommonName { get; }

        /// <summary>
        /// The locality of the certificate
        /// </summary>
        public string? Locality { get; set; }

        /// <summary>
        /// The state of the certificate
        /// </summary>
        public string? State { get; set; }
        
        /// <summary>
        /// The country of the certificate
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        ///  The organization of the certificate
        /// </summary>
        public string? Organization { get; set; }

        /// <summary>
        /// The organization unit of the certificate
        /// </summary>
        public string? OrganizationUnit { get; set; }

        /// <summary>
        ///  Number of days before the certificate expires
        /// </summary>
        public int DaysBeforeExpiration { get; set; } = 365 * 10;

        /// <summary>
        ///  A password to protect the generated p12 file
        /// </summary>
        public string? P12Password { get; set; }

        /// <summary>
        /// The key size of the certificate
        /// </summary>
        public int KeySize { get; set; } = 2048;

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(CommonName))
                throw new ArgumentException("CommonName is required");

            // KeySize must be a multiple of 1024 and less or equal to 16384
            // Control that any certificate subject attribute is valid (no comma)  

            if (KeySize % 1024 != 0 || KeySize > 16384 || KeySize < 1024)
                throw new ArgumentException("KeySize must be a multiple of 1024 and less or equal to 16384");

            if (CommonName.Contains(","))
                throw new ArgumentException("CommonName cannot contain comma");

            if (Locality != null && Locality.Contains(","))
                throw new ArgumentException("Locality cannot contain comma");

            if (Country != null && Country.Contains(","))
                throw new ArgumentException("Country cannot contain comma");

            if (Organization != null && Organization.Contains(","))
                throw new ArgumentException("Organization cannot contain comma");

            if (OrganizationUnit != null && OrganizationUnit.Contains(","))
                throw new ArgumentException("OrganizationUnit cannot contain comma");

            if (DaysBeforeExpiration <= 0)
                throw new ArgumentException("DaysBeforeExpiration cannot be negative or zero");
        }

        /// <summary>
        /// Get the certificate subject formatted
        /// </summary>
        /// <returns></returns>
        public string Format()
        {
            var builder = new StringBuilder();

            builder.Append($"CN={CommonName}");

            if (!string.IsNullOrWhiteSpace(Locality))
                builder.Append($", L={Locality}");

            if (!string.IsNullOrWhiteSpace(Country))
                builder.Append($", C={Country}");

            if (!string.IsNullOrWhiteSpace(Organization))
                builder.Append($", O={Organization}");

            if (!string.IsNullOrWhiteSpace(State))
                builder.Append($", O={State}");

            if (!string.IsNullOrWhiteSpace(OrganizationUnit))
                builder.Append($", OU={OrganizationUnit}");

            return builder.ToString();
        }
    }
}
