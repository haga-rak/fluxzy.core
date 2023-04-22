// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Ui.Logging;
using Fluxzy.Desktop.Ui.Runtime;
using Serilog;

namespace Fluxzy.Desktop.Ui
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            PrepareEnvVar();

            Log.Logger = new LoggerConfiguration()
                         .SetupLoggingConfiguration()
                         .CreateLogger();

            try {
                Log.Information("Starting fluxzyd with {CliArgs}. Configuration : {OSArchitecture} {OSDescription} " +
                                "{RuntimeIdentifier} {Memory} Mb", 
                    args, 
                    RuntimeInformation.OSArchitecture,
                    RuntimeInformation.OSDescription,
                    RuntimeInformation.RuntimeIdentifier,
                    (int) (GC.GetGCMemoryInfo().TotalAvailableMemoryBytes/ 1048576.0)
                    );

                var haltTokenSource = new CancellationTokenSource();

                AppControl.PrepareForRun(args, haltTokenSource, out var isDesktop);

                var builder = WebApplication.CreateBuilder(args);

                // Add services to the container.
                builder.Host.UseSerilog((context, services, configuration) =>
                    configuration
                        .SetupLoggingConfiguration());

                builder.Services.AddControllersWithViews().AddJsonOptions(options => {
                    foreach (var converter in GlobalArchiveOption.DefaultSerializerOptions.Converters) {
                        options.JsonSerializerOptions.Converters.Add(converter);
                    }
                });

                builder.Services.AddFluxzyDesktopServices();
                
                var app = builder.Build();

                app.UseStaticFiles();
                app.UseRouting();

                app.MapControllerRoute(
                    "default",
                    "{controller}/{action=Index}/{id?}");

                app.MapFallbackToFile("index.html");

                try {
                    await app.StartAsync(haltTokenSource.Token);

                    var globalFileManager = app.Services.GetRequiredService<FileManager>();
                    var activeRuleManages = app.Services.GetRequiredService<ActiveRuleManager>();

                    if (CommandLineUtility.TryGetArgsValue(args, "--file", out var fileName) &&
                        fileName != null)
                        await globalFileManager.Open(fileName);
                    else
                        await globalFileManager.New();

                    await activeRuleManages.InitRules();

                    if (isDesktop) {
                        await Task.Delay(1500, haltTokenSource.Token); // Wait for the UI to be ready

                        await Console.Out.WriteLineAsync("FLUXZY_LISTENING");
                        await Console.Out.FlushAsync();
                    }
                }
                catch (Exception ex) {
                    var socketException =
                        ex.FindException(e => e is SocketException sex && sex.NativeErrorCode == 10048);

                    if (socketException == null)
                        throw;

                    if (isDesktop) {
                        await Console.Out.WriteLineAsync("FLUXZY_PORT_ERROR");
                        await Console.Out.FlushAsync();

                        if (CommandLineUtility.TryGetArgsValue(args, "--file", out var fileName) &&
                            fileName != null)
                            await AppControl.AnnounceFileOpeningRequest(fileName);
                    }

                    // Instance already exists

                    return;
                }

                await app.WaitForShutdownAsync(haltTokenSource.Token);
            }
            catch (Exception ex) {
                Log.Fatal(ex, "fluxzy terminated unexpectedly");
            }
            finally {
                await Log.CloseAndFlushAsync();
            }
        }

        private static void PrepareEnvVar()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version!;

            Environment.SetEnvironmentVariable("FluxzyVersion", $"{version.Major}.{version.Minor}.{version.Build}");

#if (DEBUG)
            Environment.SetEnvironmentVariable("EnableDumpStackTraceOn502", "true");
            Environment.SetEnvironmentVariable("InsertFluxzyMetricsOnResponseHeader", "true");
#endif

            if (Environment.GetEnvironmentVariable("appdata") == null) {
                // For Linux and OSX environment this EV is missing, so we need to set it manually because it's used everywhere
                // usually XDG_DATA_HOME on linux and HOME on OSX

                Environment.SetEnvironmentVariable("appdata",
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            }
        }
    }
}
