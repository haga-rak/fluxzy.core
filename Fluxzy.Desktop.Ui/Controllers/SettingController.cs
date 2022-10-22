// Copyright © 2022 Haga Rakotoharivelo

using System.Reactive.Linq;
using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Services.Models;
using Microsoft.AspNetCore.Mvc;

namespace Fluxzy.Desktop.Ui.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingController
    {
        private readonly FluxzySettingManager _settingManager;

        public SettingController(FluxzySettingManager settingManager)
        {
            _settingManager = settingManager;
        }

        [HttpGet]
        public async Task<ActionResult<FluxzySettingsHolder>> Get()
        {
            var settingsHolder = await _settingManager.ProvidedObservable.FirstAsync();
            return settingsHolder; 
        }

        [HttpPost]
        public async Task<ActionResult<bool>> Update(FluxzySettingsHolder model)
        {
            _settingManager.Update(model);
            return true; 
        }
    }
}