using System.Reactive.Linq;
using Fluxzy.Desktop.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Fluxzy.Desktop.Services
{
    public static class GlobalRegistration
    {
        public static void AddFluxzyDesktopServices(this IServiceCollection collection)
        {
            collection.AddSingleton<FileManager>();
            collection.AddSingleton<ProxyControl>();
            collection.AddSingleton<FluxzySettingManager>();
            collection.AddSingleton<UiStateManager>();

            collection.AddSingleton<IObservable<FileState>>
                (s => s.GetRequiredService<FileManager>().Subject);

            collection.AddSingleton<IObservable<FluxzySettingsHolder>>
                (s => s.GetRequiredService<FluxzySettingManager>().Subject);

            collection.AddSingleton<IObservable<ProxyState>>
                (s => s.GetRequiredService<ProxyControl>().Subject);

            collection.AddSingleton<IObservable<FileContentOperationManager>>
                (s => s.GetRequiredService<IObservable<FileState>>().Select(v => v.ContentOperation));

            collection.AddSingleton<IObservable<TrunkState>>
                (s => s.GetRequiredService<IObservable<FileContentOperationManager>>()
                    .Select(t => t.Observable).Switch());

            collection.AddTransient<FxzyDirectoryPackager>();
        }
    }
}