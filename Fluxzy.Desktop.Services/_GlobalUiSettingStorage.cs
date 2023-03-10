// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Desktop.Services.Wizards;

namespace Fluxzy.Desktop.Services
{
    public class GlobalUiSettingStorage
    {
        private readonly string _uiSettingPath;

        public GlobalUiSettingStorage()
        {
            var applicationPath =
                Path.Combine(Environment.ExpandEnvironmentVariables("%appdata%"), "fluxzyd");

            Directory.CreateDirectory(applicationPath);

            ApplicationPath = applicationPath; 
            
            _uiSettingPath = Path.Combine(ApplicationPath, "ui-settings.json");

            UiUserSetting = LoadUiSetting(); 
        }

        private UiUserSetting LoadUiSetting()
        {
            if (!File.Exists(_uiSettingPath))
                return new UiUserSetting();

            try {
                using var fileStream = File.OpenRead(_uiSettingPath);
                return System.Text.Json.JsonSerializer.Deserialize<UiUserSetting>(fileStream)!; 
            }
            catch {
                // If error happens when loading setting, we just ignore and return the default value 
                return new UiUserSetting();
            }
        }

        public string ApplicationPath { get;  }
        
        public UiUserSetting UiUserSetting { get; private set; }
        
        
        public void UpdateUiSetting()
        {
            using var fileStream = File.OpenRead(_uiSettingPath);
            System.Text.Json.JsonSerializer.Serialize(fileStream, UiUserSetting);
            UiUserSetting = LoadUiSetting(); 
        }
    }
}
