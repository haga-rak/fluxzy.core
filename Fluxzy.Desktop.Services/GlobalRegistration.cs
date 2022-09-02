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

            collection.AddTransient<FxzyDirectoryPackager>();
        }
    }
}