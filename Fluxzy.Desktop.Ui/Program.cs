using Fluxzy;
using Fluxzy.Desktop.Services;
using System;

Environment.SetEnvironmentVariable("EnableDumpStackTraceOn502", "true");
Environment.SetEnvironmentVariable("InsertFluxzyMetricsOnResponseHeader", "true");

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
await globalFileManager.New();
await activeRuleManages.InitRules();

app.Run();
