// Copyright © 2022 Haga Rakotoharivelo

using System.Text.Json;
using Fluxzy.Desktop.Services.Models;
using Microsoft.Extensions.Configuration;

namespace Fluxzy.Desktop.Services
{
    public class FluxzySettingManager
    {
        private readonly string _settingPath;
        private FluxzySettingsHolder?  _current = null; 

        public FluxzySettingManager(IConfiguration configuration)
        {
            _settingPath = configuration["UiSettings:CaptureTemp"]
                           ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                               "Fluxzy.Desktop");

            Directory.CreateDirectory(_settingPath);
            _settingPath = Path.Combine(_settingPath, "settings.json"); 
        }


        public  void Update(FluxzySettingsHolder settingsHolder)
        {
            lock (_settingPath)
            {
                _current = settingsHolder;
                using var outStream = File.Create(_settingPath);
                JsonSerializer.Serialize(outStream, settingsHolder);
            }
        }

        public FluxzySettingsHolder Get()
        {
            if (_current != null)
                return _current; 

            if (!File.Exists(_settingPath))
            {
                return _current = new FluxzySettingsHolder()
                {
                    StartupSetting = FluxzySetting.CreateDefault()
                };
            }

            lock (_settingPath)
            {
                using var inStream = File.OpenRead(_settingPath)!;
                return (JsonSerializer.Deserialize<FluxzySettingsHolder>(inStream))!;
            }
        }
    }
}