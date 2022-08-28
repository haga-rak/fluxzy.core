using Fluxzy.Desktop.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Fluxzy.Desktop.Services
{
    public static class GlobalRegistration
    {
        public static void AddFluxzyDesktopServices(this IServiceCollection collection)
        {
            collection.AddSingleton<GlobalFileManager>();
            collection.AddSingleton<TrunkManager>();
            collection.AddSingleton<ProxyControl>();
            collection.AddSingleton<FluxzySettingManager>();

            collection.AddSingleton<IObservable<FileState>>
                (s => s.GetRequiredService<GlobalFileManager>().Observable);

            collection.AddSingleton<IObservable<FluxzySettingsHolder>>
                (s => s.GetRequiredService<FluxzySettingManager>().Observable);

            collection.AddSingleton<IObservable<ProxyState>>
                (s => s.GetRequiredService<ProxyControl>().Observable);

            collection.AddSingleton<UiStateManager>();
            collection.AddTransient<FxzyDirectoryPackager>();
        }
    }
}