// Copyright © 2022 Haga RAKOTOHARIVELO

using System.Security.Cryptography.X509Certificates;

namespace Fluxzy.Desktop.Services.Models
{
    public class CertificateValidator
    {
        public List<ValidationError> Validate(Certificate originalCertificate, out X509Certificate2? certificate)
        {
            certificate = null;

            var validationErrors = new List<ValidationError>();

            switch (originalCertificate.RetrieveMode)
            {
                case CertificateRetrieveMode.FluxzyDefault:
                {
                    certificate  = new X509Certificate2(FileStore.Fluxzy, "echoes");
                    return validationErrors;
                }

                case CertificateRetrieveMode.FromUserStoreSerialNumber:

                    if (string.IsNullOrWhiteSpace(originalCertificate.SerialNumber))
                    {
                        validationErrors.Add(new ValidationError("SerialNumber cannot be empty"));

                        return validationErrors;
                    }

                    using (var store = new X509Store(StoreLocation.CurrentUser))
                    {
                        store.Open(OpenFlags.ReadOnly);

                        var foundItems = store.Certificates.Find(X509FindType.FindBySerialNumber,
                            originalCertificate.SerialNumber, false);

                        if (!foundItems.Any())
                        {
                            validationErrors.Add(new ValidationError(
                                $"Certificate with serial number “{originalCertificate.SerialNumber}” was not found on current user store"));

                            return validationErrors;
                        }

                        certificate = foundItems.First();

                        if (!certificate.HasPrivateKey)
                        {
                            validationErrors.Add(new ValidationError(
                                $"Provided certificate {certificate.SubjectName} (SN: {originalCertificate.SerialNumber}) does not contains private key. " +
                                "Private key is mandatory for this certificate."));

                            return validationErrors;
                        }
                    }

                    break;
                case CertificateRetrieveMode.FromUserStoreThumbPrint:

                    if (string.IsNullOrWhiteSpace(originalCertificate.ThumbPrint))
                    {
                        validationErrors.Add(new ValidationError("SerialNumber cannot be empty"));

                        return validationErrors;
                    }

                    using (var store = new X509Store(StoreLocation.CurrentUser))
                    {
                        store.Open(OpenFlags.ReadOnly);

                        var foundItems = store.Certificates.Find(X509FindType.FindByThumbprint,
                            originalCertificate.ThumbPrint, false);

                        if (!foundItems.Any())
                        {
                            validationErrors.Add(new ValidationError(
                                $"Certificate with thumbprint “{originalCertificate.ThumbPrint}” was not found on current user store"));

                            return validationErrors;
                        }

                        certificate = foundItems.First();

                        if (!certificate.HasPrivateKey)
                        {
                            validationErrors.Add(new ValidationError(
                                $"Provided certificate {certificate.SubjectName} (SN: {originalCertificate.ThumbPrint}) does not contains private key. " +
                                "Private key is mandatory for this certificate."));

                            return validationErrors;
                        }
                    }

                    break;
                case CertificateRetrieveMode.FromPkcs12:

                    if (string.IsNullOrWhiteSpace(originalCertificate.Pkcs12File))
                    {
                        validationErrors.Add(new ValidationError(
                            "Pcks12File cannot be empty"));

                        return validationErrors;
                    }

                    if (!File.Exists(originalCertificate.Pkcs12File))
                    {
                        validationErrors.Add(new ValidationError(
                            $"File {new FileInfo(originalCertificate.Pkcs12File).FullName} was not found"));

                        return validationErrors;
                    }

                    var byteContent = File.ReadAllBytes(originalCertificate.Pkcs12File);

                    try
                    {
                        certificate =
                            string.IsNullOrEmpty(originalCertificate.Pkcs12Password)
                                ? new X509Certificate2(byteContent)
                                : new X509Certificate2(byteContent, originalCertificate.Pkcs12Password.AsSpan());
                    }
                    catch (Exception ex)
                    {
                        validationErrors.Add(new ValidationError(
                            $"{ex.Message}"));

                        return validationErrors;
                    }

                    if (!certificate.HasPrivateKey)
                    {
                        validationErrors.Add(new ValidationError(
                            $"Provided certificate  “{certificate.SubjectName.Name}” does not come with private key. Private key is mandatory for this certificate."));

                        return validationErrors;
                    }

                    break;
                default:
                {
                    validationErrors.Add(new ValidationError("Invalid location option"));

                    return validationErrors;

                    ;
                }
            }

            // TODO add more control about certificate
            // (like check alternative names and common name)

            return validationErrors;
        }
    }

    public class CertificateValidationResult
    {
        public string? SubjectName { get; set; }

        public List<ValidationError> Errors { get; set; }
    }

    public class ValidationError
    {
        public string Message { get; }

        public ValidationError(string message)
        {
            Message = message;
        }
    }
}
