using System.Reactive.Linq;
using Fluxzy.Core;
using Fluxzy.Core.Proxy;
using Fluxzy.Desktop.Services.Filters;
using Fluxzy.Desktop.Services.Filters.Implementations;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Desktop.Services.Rules;
using Fluxzy.Desktop.Services.Ui;
using Fluxzy.Formatters;
using Fluxzy.Formatters.Producers.ProducerActions.Actions;
using Fluxzy.NativeOp;
using Fluxzy.Readers;
using Fluxzy.Utils;
using Fluxzy.Utils.Curl;
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
            collection.AddSingleton<FileContentUpdateManager>();
            collection.AddSingleton<ToolBarFilterProvider>();
            collection.AddSingleton<TemplateToolBarFilterProvider>();
            collection.AddSingleton<ForwardMessageManager>();
            collection.AddSingleton<IRuleStorage, LocalRuleStorage>();
            collection.AddSingleton<ActiveRuleManager>();
            collection.AddSingleton<FileDynamicStatsManager>();
            collection.AddSingleton<LastOpenFileManager>();
            

            collection.AddSingleton
                (s => s.GetRequiredService<SystemProxyStateControl>().ProvidedObservable);

            collection.AddSingleton
                (s => s.GetRequiredService<FileManager>().ProvidedObservable);

            collection.AddSingleton<IObservable<IArchiveReader>>
            (s => s.GetRequiredService<IObservable<FileState>>()
                   .Select(f => new DirectoryArchiveReader(f.WorkingDirectory)));

            collection.AddSingleton
                (s => s.GetRequiredService<FluxzySettingManager>().ProvidedObservable);

            collection.AddSingleton
                (s => s.GetRequiredService<ProxyControl>().ProvidedObservable);

            collection.AddSingleton
                (s => s.GetRequiredService<ProxyControl>().WriterObservable);

            collection.AddSingleton
                (s => s.GetRequiredService<ActiveViewFilterManager>().ProvidedObservable);

            collection.AddSingleton
                (s => s.GetRequiredService<FilteredExchangeManager>().ProvidedObservable);
            
            collection.AddSingleton
                (s => s.GetRequiredService<FileDynamicStatsManager>().ProvidedObservable);
            
            collection.AddSingleton
                (s => s.GetRequiredService<LastOpenFileManager>().ProvidedObservable);

            collection.AddSingleton
                (s => s.GetRequiredService<ActiveRuleManager>().ActiveRules);

            collection.AddSingleton
                (s => s.GetRequiredService<TemplateToolBarFilterProvider>().ProvidedObservable);

            collection.AddSingleton
                (s => s.GetRequiredService<IObservable<FileState>>().Select(v => v.ContentOperation));
            
            collection.AddSingleton
            (s => s.GetRequiredService<IObservable<FileContentOperationManager>>()
                   .Select(t => t.Observable).Switch());

            collection.AddScoped<IArchiveReaderProvider, ArchiveReaderProvider>();
            collection.AddScoped<FilterTemplateManager>();
            collection.AddScoped<ContextMenuActionProvider>();
            collection.AddScoped<ContextMenuFilterProvider>();
            collection.AddScoped<ActionTemplateManager>();
            collection.AddScoped<CertificateValidator>();
            collection.AddScoped<SystemService>();
            collection.AddScoped<CurlRequestConverter>();
            collection.AddScoped<IRequestReplayManager, CurlRequestReplayManager>();
            collection.AddSingleton<CurlExportFolderManagement>(_ => new CurlExportFolderManagement());
            collection.AddScoped<FileExecutionManager>();
            collection.AddScoped<IRunningProxyProvider, RunningProxyProvider>();
            
            collection.AddSingleton<ISystemProxySetterManager, NativeProxySetterManager>(); // TODO, replace here with pipe call 
            collection.AddSingleton<ISystemProxySetter>((i) => i.GetRequiredService<ISystemProxySetterManager>().Get()); 
            collection.AddSingleton<SystemProxyRegistrationManager>();

            collection.AddTransient<FxzyDirectoryPackager>();

            collection.AddTransient<FormatSettings>(); // TODO move to hard settings 

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
            serviceCollection.AddScoped<SaveWebSocketBodyAction>();
            serviceCollection.AddScoped<SaveRawCaptureAction>();

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
