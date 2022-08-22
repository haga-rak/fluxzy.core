// Copyright © 2022 Haga Rakotoharivelo

using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Fluxzy.Desktop.Services
{
    public class FluxzySettingManager
    {
        private readonly string _settingPath;

        public FluxzySettingManager(IConfiguration configuration)
        {
            _settingPath = configuration["UiSettings:CaptureTemp"]
                           ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                               "Fluxzy.Desktop");

            Directory.CreateDirectory(_settingPath);
            _settingPath = Path.Combine(_settingPath, "settings.json"); 
        }

        public  async Task Update(FluxzySettingsHolder settingsHolder)
        {
            await using var outStream = File.Create(_settingPath);
            await JsonSerializer.SerializeAsync(outStream, settingsHolder);
        }

        public async Task<FluxzySettingsHolder> Get()
        {
            if (!File.Exists(_settingPath))
                return new FluxzySettingsHolder()
                {
                    StartupSetting = FluxzySetting.CreateDefault()
                };

            await using var inStream = File.OpenRead(_settingPath)!; 

            return (await JsonSerializer.DeserializeAsync<FluxzySettingsHolder>(inStream))!; 
        }
    }
}