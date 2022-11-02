// Copyright © 2022 Haga RAKOTOHARIVELO

using System.Security.Cryptography.X509Certificates;

namespace Fluxzy.Desktop.Services.Models
{
    public class CertificateViewModel
    {
        public CertificateLocation Location { get; set; } = CertificateLocation.Store; 

        public string?  CertificateSerialNumber { get; set; }

        public string? Pkcs12File { get; set; }

        public string Password { get; set; }

        public IEnumerable<ValidationError> Validate()
        {
            X509Certificate2 ?  certificate = null; 

            switch (Location)
            {
                case CertificateLocation.Store:

                    if (string.IsNullOrWhiteSpace(CertificateSerialNumber))
                    {
                        yield return new ValidationError("Fingerprint cannot be empty");
                        yield break; 
                    }

                    using (var store = new X509Store(StoreLocation.CurrentUser))
                    {
                        var foundItems = store.Certificates.Find(X509FindType.FindBySerialNumber,
                            CertificateSerialNumber, false);

                        if (!foundItems.Any())
                        {
                            yield return new ValidationError(
                                $"Certificate with sn {CertificateSerialNumber} was not found on current user store");

                            yield break; 
                        }

                        certificate = foundItems.First();

                        if (!certificate.HasPrivateKey)
                        {
                            yield return new ValidationError(
                                $"Provided certificate {certificate.SubjectName} (SN: {CertificateSerialNumber}) does not contains private key. Private key is mandatory for this certificate.");

                            yield break; 
                        }
                    }
                    break;
                case CertificateLocation.FromPkcs12File:

                    if (string.IsNullOrWhiteSpace(Pkcs12File))
                    {
                        yield return new ValidationError(
                            $"Pcks12File cannot be empty");

                        yield break; 
                    }

                    if (!File.Exists(Pkcs12File))
                    {
                        yield return new ValidationError(
                            $"File {new FileInfo(Pkcs12File).FullName} was not found");

                        yield break; 
                    }

                    var byteContent = File.ReadAllBytes(Pkcs12File);

                    certificate =
                        string.IsNullOrEmpty(Password)
                            ? new X509Certificate2(byteContent)
                            : new X509Certificate2(byteContent, Password.AsSpan());

                    if (certificate.HasPrivateKey)
                    {
                        yield return new ValidationError(
                            $"Provided certificate {certificate.SubjectName} does not come with private key. Private key is mandatory for this certificate.");

                        yield break;
                    }
                    break;
                default:
                {
                    yield return new ValidationError($"Invalid location option"); 
                    yield break;

                    ;
                }
            }

            // TODO add more control about certificate
            // (like check alternative names and common name)

            certificate.Dispose();

           
        }
    }

    public enum CertificateLocation
    {
        Store = 1 , 
        FromPkcs12File
    }

    public class ValidationError
    {
        public ValidationError(string message)
        {
            Message = message;
        }

        public string Message { get;  }
    }
}
