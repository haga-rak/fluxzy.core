// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Desktop.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProxyController : ControllerBase
    {
        private readonly SystemProxyStateControl _systemProxyStateControl;

        public ProxyController(SystemProxyStateControl systemProxyStateControl)
        {
            _systemProxyStateControl = systemProxyStateControl;
        }

        [HttpPost("on")]
        public ActionResult<bool> On()
        {
            _systemProxyStateControl.On();

            return true;
        }

        [HttpPost("off")]
        public ActionResult<bool> Off()
        {
            _systemProxyStateControl.Off();

            return true;
        }
    }
}
