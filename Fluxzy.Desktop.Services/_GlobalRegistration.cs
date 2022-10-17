using System.Reactive.Linq;
using Fluxzy.Desktop.Services.Filters;
using Fluxzy.Desktop.Services.Filters.Implementations;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Formatters;
using Fluxzy.Formatters.Producers.ProducerActions.Actions;
using Fluxzy.Formatters.Producers.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace Fluxzy.Desktop.Services
{
    public static class GlobalRegistration
    {
        public static IServiceCollection AddFluxzyDesktopServices(this IServiceCollection collection)
        {
            collection.AddSingleton<FileManager>();
            collection.AddSingleton<ProxyControl>();
            collection.AddSingleton<FluxzySettingManager>();
            collection.AddSingleton<UiStateManager>();
            collection.AddSingleton<SystemProxyStateControl>();
            collection.AddSingleton<ActiveViewFilterManager>();
            collection.AddSingleton<FilteredExchangeManager>();
            collection.AddSingleton<ToolBarFilterProvider>();

            collection.AddSingleton<IObservable<SystemProxyState>>
                (s => s.GetRequiredService<SystemProxyStateControl>().Observable);

            collection.AddSingleton<IObservable<FileState>>
                (s => s.GetRequiredService<FileManager>().Observable);

            collection.AddSingleton<IObservable<FluxzySettingsHolder>>
                (s => s.GetRequiredService<FluxzySettingManager>().Observable);

            collection.AddSingleton<IObservable<ProxyState>>
                (s => s.GetRequiredService<ProxyControl>().Observable);

            collection.AddSingleton<IObservable<ViewFilter>>
                (s => s.GetRequiredService<ActiveViewFilterManager>().Observable);

            collection.AddSingleton<IObservable<FilteredExchangeState?>>
                (s => s.GetRequiredService<FilteredExchangeManager>().Observable);

            collection.AddSingleton<IObservable<FileContentOperationManager>>
                (s => s.GetRequiredService<IObservable<FileState>>().Select(v => v.ContentOperation));

            collection.AddSingleton<IObservable<TrunkState>>
                (s => s.GetRequiredService<IObservable<FileContentOperationManager>>()
                    .Select(t => t.Observable).Switch());


            collection.AddScoped<IArchiveReaderProvider, ArchiveReaderProvider>();
            collection.AddScoped<FilterTemplateManager>();
            collection.AddScoped<ContextMenuActionProvider>();
            collection.AddScoped<ContextMenuFilterProvider>();

            collection.AddTransient<FxzyDirectoryPackager>();

            collection.AddTransient<ProducerSettings>(); // TODO move to hard settings 

            collection.AddFluxzyProducers();

            collection.AddViewFilters();

            return collection; 
        }

        public static IServiceCollection AddFluxzyProducers(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ProducerFactory>();
            serviceCollection.AddScoped<SaveRequestBodyProducerAction>();
            serviceCollection.AddScoped<SaveFileMultipartAction>();
            serviceCollection.AddScoped<SaveResponseBodyAction>();

            return serviceCollection;

        }

        public static IServiceCollection AddViewFilters(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ViewFilterManagement>();
            serviceCollection.AddSingleton<LocalFilterStorage>();
            serviceCollection.AddSingleton<InSessionFileStorage>();

            return serviceCollection;

        }
    }
}