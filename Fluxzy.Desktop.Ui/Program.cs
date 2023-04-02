using System.Net.Sockets;
using System.Reflection;
using Fluxzy;
using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Ui.Runtime;

var version = Assembly.GetExecutingAssembly().GetName().Version!;

Environment.SetEnvironmentVariable("FluxzyVersion", $"{version.Major}.{version.Minor}.{version.Build}");

Environment.SetEnvironmentVariable("EnableDumpStackTraceOn502", "true");
Environment.SetEnvironmentVariable("InsertFluxzyMetricsOnResponseHeader", "true");

if (Environment.GetEnvironmentVariable("appdata") == null)
{
    // For Linux and OSX environment this EV is missing, so we need to set it manually 
    // to XDG_DATA_HOME
    
    Environment.SetEnvironmentVariable("appdata", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
}

var haltTokenSource = new CancellationTokenSource(); 

AppControl.PrepareForRun(args, haltTokenSource, out var isDesktop);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews().AddJsonOptions(options =>
{
    foreach (var converter in GlobalArchiveOption.DefaultSerializerOptions.Converters)
        options.JsonSerializerOptions.Converters.Add(converter);
});

builder.Services.AddFluxzyDesktopServices();

//builder.Services.AddSignalR().AddJsonProtocol(
//    options =>
//        options.PayloadSerializerOptions = GlobalArchiveOption.DefaultSerializerOptions
//);

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
    {
        await globalFileManager.Open(fileName);
    }
    else
    {
        await globalFileManager.New();
    }

    await activeRuleManages.InitRules();

    if (isDesktop) {
        Console.Out.WriteLine("FLUXZY_LISTENING");
        Console.Out.Flush();
    }
}
catch (Exception ex) {
    var socketException = ex.FindException(e => e is SocketException sex && sex.NativeErrorCode == 10048); 
    
    if (socketException == null)
        throw;

    if (isDesktop)
    {
        Console.Out.WriteLine("FLUXZY_PORT_ERROR");
        Console.Out.Flush();

        if (CommandLineUtility.TryGetArgsValue(args, "--file", out var fileName) &&
            fileName != null)
        {
            await AppControl.AnnounceFileOpeningRequest(fileName);
        }
        
        // This is a classic port error. 
        
    }

    return;
}

await app.WaitForShutdownAsync(haltTokenSource.Token); 
