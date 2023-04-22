// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Services.Models;
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
    }
}
