// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Desktop.Services
{
    public interface ISettingManager
    {
        Task Update(FluxzySettingsHolder settingsHolder);

        Task<FluxzySettingsHolder> Get();
    }

    public class FluxzySettingsHolder
    {
        public FluxzySetting StartupSetting { get; set; } = new();
    }
}