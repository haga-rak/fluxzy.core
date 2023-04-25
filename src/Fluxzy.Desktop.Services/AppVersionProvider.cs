using Fluxzy.Desktop.Services.Models;

namespace Fluxzy.Desktop.Services
{
    public class AppVersionProvider
    {
        public AppVersionProvider()
        {
            Version = new AppVersion(
                Environment.GetEnvironmentVariable("FluxzyVersion") ?? "n/c",
                Environment.GetEnvironmentVariable("FluxzyEngineVersion") ?? "n/c",
                Environment.GetEnvironmentVariable("FluxzyDesktopVersion") ?? "n/c"
            ); 
        }

        public AppVersion Version { get; }
    }
}
