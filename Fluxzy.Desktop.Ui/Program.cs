using Fluxzy;
using Fluxzy.Desktop.Services;
using Fluxzy.Desktop.Services.Hubs;
using Microsoft.AspNetCore.Routing;

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
builder.Services.AddSignalR(a => a.ClientTimeoutInterval = TimeSpan.FromSeconds(5));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

//app.UseEndpoints((endpointRouteBuilder) =>
//{
//    endpointRouteBuilder.MapHub<GlobalHub>("/xs/ui-state-update");
//});

app.MapHub<GlobalHub>("/xs");
app.MapFallbackToFile("index.html");

var globalFileManager = app.Services.GetRequiredService<GlobalFileManager>();
//await globalFileManager.Off();
await globalFileManager.Open(@"../Samples/boot.fxzy");

app.Run();