// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Linq;
using System.Text.Json;
using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Desktop.Services.UiSettings;
using Fluxzy.Desktop.Ui.ViewModels;
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
        
        [HttpGet("ui/{key}/contains")]
        public ActionResult<bool> Has(string key, [FromServices] UiSettingHolder settingHolder)
        {
            return settingHolder.HasKey(key); 
        }
        
        [HttpPut("ui/{key}")]
        public ActionResult<bool> Update(string key, [FromBody]
            UiSetting element, [FromServices] UiSettingHolder settingHolder)
        {
            return settingHolder.Update(key, element.Value);
        }
        
        [HttpGet("ui/{key}")]
        public ActionResult<UiSetting> Get(string key, 
            [FromServices] UiSettingHolder settingHolder)
        {
            if (!settingHolder.TryGet(key, out var settingValue)) {
                return new NotFoundObjectResult(key); 
            }

            return new UiSetting(settingValue!);
        }


        [HttpGet("endpoint")]
        public ActionResult<List<NetworkInterfaceInfo>> AvailableEndpoints()
        {
            return NetworkInterfaceInfoProvider.GetNetworkInterfaceInfos();
        }
    }
}
