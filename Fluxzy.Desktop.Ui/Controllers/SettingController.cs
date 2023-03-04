// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Linq;
using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Misc.IpUtils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

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

            // Update view model 
            settingsHolder.UpdateViewModel();

            return settingsHolder;
        }

        [HttpPost]
        public ActionResult<bool> Update([ValidateNever] FluxzySettingsHolder model)
        {
            model.UpdateModel();
            _settingManager.Update(model);

            return true;
        }

        [HttpGet("endpoint")]
        public ActionResult<List<NetworkInterfaceInfo>> AvailableEndpoints()
        {
            return NetworkInterfaceInfoProvider.GetNetworkInterfaceInfos();
        }
    }
}
