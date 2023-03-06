// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Certificates;
using Fluxzy.Desktop.Services.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/extended-control")]
    [ApiController]
    public class ExtendedControlController : ControllerBase
    {
        [HttpPost("certificate")]
        public ActionResult<CertificateValidationResult> ValidateCertificate(
            [FromBody] Certificate certificate, [FromServices] CertificateValidator validator)
        {
            var errors = validator.Validate(certificate, out var outCertificate);

            if (errors.Any()) {
                return new CertificateValidationResult {
                    Errors = errors
                };
            }

            return new CertificateValidationResult {
                SubjectName = outCertificate!.SubjectName.Name
            };
        }
    }
}
