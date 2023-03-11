// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Desktop.Services.Wizards;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WizardController
    {
        private readonly CertificateWizard _certificateWizard;

        public WizardController(CertificateWizard certificateWizard)
        {
            _certificateWizard = certificateWizard;
        }

        [HttpGet("certificate/check")]
        public async Task<ActionResult<CertificateWizardStatus>> ShouldAskCertificateWizard()
        {
            return await _certificateWizard.ShouldAskCertificateWizard();
        }

        [HttpPost("certificate/install")]
        public async Task<ActionResult<bool>> InstallCertificate()
        {
            return await _certificateWizard.InstallCertificate();
        }

        [HttpPost("certificate/refuse")]
        public ActionResult<bool> RefuseCertificate()
        {
            _certificateWizard.RefuseCertificate();

            return true;
        }

        /// <summary>
        ///     Undo the refuse certificate wizard
        /// </summary>
        /// <returns></returns>
        [HttpPost("certificate/revive")]
        public ActionResult<bool> ReviveWizard()
        {
            _certificateWizard.ReviveWizard();

            return true;
        }
    }
}
