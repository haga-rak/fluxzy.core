using System.Net.Sockets;
using Fluxzy;
using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Ui.Runtime;

Environment.SetEnvironmentVariable("EnableDumpStackTraceOn502", "true");
Environment.SetEnvironmentVariable("InsertFluxzyMetricsOnResponseHeader", "true");

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

builder.Services.AddSignalR().AddJsonProtocol(
    options =>
        options.PayloadSerializerOptions = GlobalArchiveOption.DefaultSerializerOptions
);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    "default",
    "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

var globalFileManager = app.Services.GetRequiredService<FileManager>();
var activeRuleManages = app.Services.GetRequiredService<ActiveRuleManager>();
//await globalFileManager.Off();

// await globalFileManager.Open(@"../Samples/boot.fxzy");

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

try {
    await app.StartAsync(haltTokenSource.Token);

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
    }

    return;
}

await app.WaitForShutdownAsync(haltTokenSource.Token); 
