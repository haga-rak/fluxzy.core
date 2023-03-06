// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json.Serialization;

namespace Fluxzy.Desktop.Services.Models
{
    public class FluxzySettingsHolder
    {
        [JsonConstructor]
        public FluxzySettingsHolder(FluxzySetting startupSetting)
        {
            StartupSetting = startupSetting;
        }

        public FluxzySetting StartupSetting { get; }

        public FluxzySettingViewModel? ViewModel { get; set; }

        public void UpdateViewModel()
        {
            ViewModel = new FluxzySettingViewModel(StartupSetting);
        }

        public void UpdateModel()
        {
            ViewModel?.ApplyToSetting(StartupSetting);
        }
    }
}
