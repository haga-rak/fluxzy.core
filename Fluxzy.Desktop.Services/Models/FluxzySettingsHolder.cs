// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Desktop.Services.Models
{
    public class FluxzySettingsHolder
    {
        public FluxzySettingsHolder()
        {

        }

        public FluxzySettingsHolder(FluxzySetting startupSetting) : this()
        {
            StartupSetting = startupSetting;
        }

        public FluxzySetting StartupSetting { get; set; } = new();
    }
}