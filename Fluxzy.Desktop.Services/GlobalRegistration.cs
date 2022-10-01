using System.Reactive.Linq;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Screeners;
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
            collection.AddSingleton<SystemProxyStateControl>();

            collection.AddSingleton<IObservable<SystemProxyState>>
                (s => s.GetRequiredService<SystemProxyStateControl>().Observable);

            collection.AddSingleton<IObservable<FileState>>
                (s => s.GetRequiredService<FileManager>().Observable);

            collection.AddSingleton<IObservable<FluxzySettingsHolder>>
                (s => s.GetRequiredService<FluxzySettingManager>().Observable);

            collection.AddSingleton<IObservable<ProxyState>>
                (s => s.GetRequiredService<ProxyControl>().Observable);

            collection.AddSingleton<IObservable<FileContentOperationManager>>
                (s => s.GetRequiredService<IObservable<FileState>>().Select(v => v.ContentOperation));

            collection.AddSingleton<IObservable<TrunkState>>
                (s => s.GetRequiredService<IObservable<FileContentOperationManager>>()
                    .Select(t => t.Observable).Switch());

            collection.AddTransient<FxzyDirectoryPackager>();
        }

        public static void AddFluxzyProducers(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ProducerFactory>(); 

        }
    }
}