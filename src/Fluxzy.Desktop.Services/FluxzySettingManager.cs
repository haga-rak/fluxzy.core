// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Subjects;
using System.Text.Json;
using Fluxzy.Desktop.Services.Models;
using Microsoft.Extensions.Configuration;

namespace Fluxzy.Desktop.Services
{
    public class FluxzySettingManager : ObservableProvider<FluxzySettingsHolder>
    {
        private readonly BehaviorSubject<FluxzySettingsHolder> _internalSubject;
        private readonly string _settingPath;

        public FluxzySettingManager(IConfiguration configuration)
        {
            _settingPath = configuration["UiSettings:CaptureTemp"]
                           ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                               "Fluxzy.Desktop");

            Directory.CreateDirectory(_settingPath);
            _settingPath = Path.Combine(_settingPath, "settings.json");

            _internalSubject = new BehaviorSubject<FluxzySettingsHolder>(ReadFromFile());
            Subject = _internalSubject;
        }

        protected override BehaviorSubject<FluxzySettingsHolder> Subject { get; }

        public void Update(FluxzySettingsHolder settingsHolder)
        {
            lock (_settingPath) {
                using var outStream = File.Create(_settingPath);
                JsonSerializer.Serialize(outStream, settingsHolder, GlobalArchiveOption.DefaultSerializerOptions);
            }

            _internalSubject.OnNext(settingsHolder);
        }

        private FluxzySettingsHolder ReadFromFile()
        {
            if (!File.Exists(_settingPath))
                return new FluxzySettingsHolder(FluxzySetting.CreateDefault());

            lock (_settingPath) {
                using var inStream = File.OpenRead(_settingPath)!;

                return JsonSerializer.Deserialize<FluxzySettingsHolder>(inStream,
                    GlobalArchiveOption.DefaultSerializerOptions)!;
            }
        }
    }
}
