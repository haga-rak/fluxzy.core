// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Desktop.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProxyController : ControllerBase
    {
        private readonly ProxyControl _proxyControl;

        public ProxyController(ProxyControl proxyControl)
        {
            _proxyControl = proxyControl;
        }

        [HttpPost("on")]
        public async Task<ActionResult<bool>> On()
        {
            await _proxyControl.SetAsSystemProxy();
            return true; 
        }

        [HttpPost("off")]
        public async Task<ActionResult<bool>> Off()
        {
            await _proxyControl.UnsetAsSystemProxy();
            return true; 
        }
    }
}