using Microsoft.Extensions.DependencyInjection;

namespace Fluxzy.Desktop.Services
{
    public static class GlobalRegistration
    {
        public static void AddFluxzyDesktopServices(this IServiceCollection collection)
        {
            collection.AddSingleton<GlobalFileManager>();
            collection.AddSingleton<FileSessionManager>();
            collection.AddSingleton<ProxyControl>();
            collection.AddSingleton<FluxzySettingManager>();

            collection.AddScoped<UiStateManager>();
        }
    }
}