// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Text;

namespace Fluxzy.Certificates
{
    public class CertificateBuilderOptions
    {
        public CertificateBuilderOptions(string commonName)
        {
            CommonName = commonName;
        }

        public string CommonName { get; }

        public string? Locality { get; set; }

        public string? State { get; set; }

        public string? Country { get; set; }

        public string? Organization { get; set; }

        public string? OrganizationUnit { get; set; }

        public int DaysBeforeExpiration { get; set; } = 365 * 10;

        public string? P12Password { get; set; }

        public int KeySize { get; set; }

        public void Validate()
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
