// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Linq;
using Fluxzy.Certificates;
using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Desktop.Ui.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemController
    {
        private readonly SystemService _systemService;

        public SystemController(SystemService systemService)
        {
            _systemService = systemService;
        }

        [HttpGet("certificates")]
        public ActionResult<List<CertificateOnStore>> GetStoreCertificates([FromQuery] bool caOnly = false)
        {
            return _systemService.GetStoreCertificates(caOnly);
        }

        [HttpPost("certificates/save")]
        public async Task<ActionResult<bool>> SaveCurrentCaToFile(
            FileSaveViewModel model, [FromServices] FluxzySettingManager settingManager)
        {
            var setting = await settingManager.ProvidedObservable.FirstAsync();

            var certificate = setting.StartupSetting.CaCertificate.GetX509Certificate();
            var pem = certificate.ExportToPem();

            await File.WriteAllBytesAsync(model.FileName, pem);

            return true;
        }

        [HttpGet("version")]
        public ActionResult<AppVersion> GetVersion([FromServices] AppVersionProvider provider)
        {
            return provider.Version;
        }
    }
}
