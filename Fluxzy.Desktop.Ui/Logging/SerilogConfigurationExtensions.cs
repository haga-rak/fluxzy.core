// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Serilog;
using Serilog.Events;

namespace Fluxzy.Desktop.Ui.Logging
{
    internal static class SerilogConfigurationExtensions
    {
        public static LoggerConfiguration SetupLoggingConfiguration(this LoggerConfiguration configuration)
        {
            return configuration
                   .MinimumLevel.Information()
                   .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                   .Enrich.With(new EnvironmentInformationEnricher())
                   .Enrich.With(new DeletePropertiesEnricher())
                   .WriteTo.File(
                       Environment.ExpandEnvironmentVariables(
                           "%appdata%/Fluxzy.Desktop/logs/fluxzy.log.txt"),
                       rollingInterval: RollingInterval.Day,
                       rollOnFileSizeLimit: true,
                       fileSizeLimitBytes: 1024 * 512)
                   .WriteTo.Seq("https://logs.fluxzy.io",
                       messageHandler: new HttpClientHandler()
                       {
                           Proxy = null,
                           UseProxy = false,
                       },
                       restrictedToMinimumLevel: LogEventLevel.Information,
                       apiKey: "vMmUtrjFR2Vue5ZcKkuqttTpUDfh5hqNkB4yuveVLH7W3c2UkC");
        }
    }
}
