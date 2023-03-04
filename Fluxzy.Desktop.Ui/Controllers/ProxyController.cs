// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Linq;
using Fluxzy.Desktop.Services;
using Fluxzy.Rules.Filters;
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
        public async Task<ActionResult<bool>> On([FromServices] FluxzySettingManager settingManager)
        {
            // Erase save filter when startup normally
            var settingsHolder = await settingManager.ProvidedObservable.FirstAsync();
            settingsHolder.StartupSetting.SaveFilter = null;
            settingManager.Update(settingsHolder);

            _systemProxyStateControl.On();

            return true;
        }

        [HttpPost("on/with-settings")]
        public async Task<ActionResult<bool>> OnWithSetting(
            [FromServices] FluxzySettingManager settingManager,
            [FromBody]
            Filter? saveFilter = null)
        {
            var settingsHolder = await settingManager.ProvidedObservable.FirstAsync();
            settingsHolder.StartupSetting.SaveFilter = saveFilter;
            settingManager.Update(settingsHolder);

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
