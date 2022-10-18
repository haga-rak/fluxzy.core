using Fluxzy;
using Fluxzy.Clients;
using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Services.Hubs;

Environment.SetEnvironmentVariable("EnableDumpStackTraceOn502", "true");
Environment.SetEnvironmentVariable("InsertFluxzyMetricsOnResponseHeader", "true");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services.AddControllersWithViews().AddJsonOptions(options =>
{
    foreach (var converter in GlobalArchiveOption.JsonSerializerOptions.Converters)
    {
        options.JsonSerializerOptions.Converters.Add(converter);
    }
});

builder.Services.AddFluxzyDesktopServices();
builder.Services.AddSignalR().AddJsonProtocol(
    options => 
        options.PayloadSerializerOptions = GlobalArchiveOption.JsonSerializerOptions
    );

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");

var globalFileManager = app.Services.GetRequiredService<FileManager>();
//await globalFileManager.Off();
await globalFileManager.Open(@"../Samples/boot.fxzy");

app.Run();