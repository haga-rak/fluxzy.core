using System.Text.Json;

namespace Fluxzy.Desktop.Services.UiSettings
{
    public class UiSettingHolder
    {
        private readonly string _fullPath;
        private readonly Dictionary<string, object> _localSettings  = new();

        public UiSettingHolder()
        {
            var basePath = Environment.ExpandEnvironmentVariables("%appdata%/Fluxzy.Desktop");
            Directory.CreateDirectory(basePath);
            _fullPath = Path.Combine(basePath, "settings-ui.json");

            lock (this) {
                if (File.Exists(_fullPath))
                {
                    try
                    {
                        _localSettings =
                            JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(_fullPath))!;
                    }
                    catch
                    {
                        // We ignore the error and create a new file.
                        _localSettings = new();
                    }
                }
            }
        }

        public bool HasKey(string key)
        {
            return _localSettings.ContainsKey(key); 
        }
        
        public bool TryGet<T>(string key, out T? result) where T : class
        {
            result = default; 
            
            if (_localSettings.TryGetValue(key, out var @object)) {
                result = (T) @object; 
                return true; 
            }

            return false; 
        }

        public bool Update<T>(string key, T value) where T : class
        {
            lock (this)
            {
                _localSettings[key] = value;

                var flatContent = JsonSerializer.Serialize(_localSettings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_fullPath, flatContent);
            }

            return true; 
        }
    }
}
