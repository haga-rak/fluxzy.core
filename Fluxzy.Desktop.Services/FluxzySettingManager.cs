// Copyright © 2022 Haga Rakotoharivelo

using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Fluxzy.Desktop.Services.Models;
using Microsoft.Extensions.Configuration;

namespace Fluxzy.Desktop.Services
{
    public class FluxzySettingManager : IObservableProvider<FluxzySettingsHolder>
    {
        private readonly string _settingPath;

        private readonly BehaviorSubject<FluxzySettingsHolder> _internalSubject;

        public FluxzySettingManager(IConfiguration configuration)
        {
            _settingPath = configuration["UiSettings:CaptureTemp"]
                           ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                               "Fluxzy.Desktop");

            Directory.CreateDirectory(_settingPath);
            _settingPath = Path.Combine(_settingPath, "settings.json");

            _internalSubject = new BehaviorSubject<FluxzySettingsHolder>(ReadFromFile());
            Observable = _internalSubject.AsObservable();
            Observable.Do(settingHolder => Current = settingHolder).Subscribe();
        }

        public FluxzySettingsHolder?  Current { get; private set; }

        public IObservable<FluxzySettingsHolder> Observable { get; }

        public void Update(FluxzySettingsHolder settingsHolder)
        {
            lock (_settingPath)
            {
                using var outStream = File.Create(_settingPath);
                JsonSerializer.Serialize(outStream, settingsHolder);
            }

            _internalSubject.OnNext(settingsHolder);
        }

        private FluxzySettingsHolder ReadFromFile()
        {
            if (!File.Exists(_settingPath))
            {
                return new FluxzySettingsHolder(startupSetting: FluxzySetting.CreateDefault());
            }

            lock (_settingPath)
            {
                using var inStream = File.OpenRead(_settingPath)!;
                return (JsonSerializer.Deserialize<FluxzySettingsHolder>(inStream))!;
            }
        }

    }

}