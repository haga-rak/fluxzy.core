// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.Json;
using Fluxzy.Desktop.Services.Wizards;

namespace Fluxzy.Desktop.Services
{
    public class GlobalUiSettingStorage
    {
        private readonly string _uiSettingPath;

        public GlobalUiSettingStorage()
        {
            var applicationPath =
                Path.Combine(Environment.ExpandEnvironmentVariables("%appdata%"), "Fluxzy.Desktop");

            Directory.CreateDirectory(applicationPath);

            ApplicationPath = applicationPath;

            _uiSettingPath = Path.Combine(ApplicationPath, "settings-env.json");

            UiUserSetting = LoadUiSetting();
        }

        public string ApplicationPath { get; }

        public UiUserSetting UiUserSetting { get; private set; }

        private UiUserSetting LoadUiSetting()
        {
            if (!File.Exists(_uiSettingPath))
                return new UiUserSetting();

            try {
                using var fileStream = File.OpenRead(_uiSettingPath);

                return JsonSerializer.Deserialize<UiUserSetting>(fileStream)!;
            }
            catch {
                // If error happens when loading setting, we just ignore and return the default value 
                return new UiUserSetting();
            }
        }

        public void UpdateUiSetting()
        {
            using (var fileStream = File.Create(_uiSettingPath)) {
                JsonSerializer.Serialize(fileStream, UiUserSetting);
            }

            UiUserSetting = LoadUiSetting();
        }
    }
}
