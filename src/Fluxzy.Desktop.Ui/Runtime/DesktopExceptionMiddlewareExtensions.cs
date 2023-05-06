// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Text.Json;
using Fluxzy.Desktop.Services;
using Microsoft.AspNetCore.Diagnostics;

namespace Fluxzy.Desktop.Ui.Runtime
{
    public static class DesktopExceptionMiddlewareExtensions
    {
        public static void ConfigureDesktopExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json";

                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();

                    if (contextFeature?.Error is DesktopException desktopException) {

                        context.Response.StatusCode = 490; // Custom status code for desktop exception

                        await context.Response.WriteAsync(
                            JsonSerializer.Serialize(desktopException.DesktopMessage, 
                                new JsonSerializerOptions(JsonSerializerDefaults.Web)));

                        return; 
                    }
#if (DEBUG)

                    var fullBodyError = new DesktopErrorMessage(contextFeature?.Error?.ToString()
                                                                ?? "Unknown error");

                    await context.Response.WriteAsync(
                        JsonSerializer.Serialize(fullBodyError,
                            new JsonSerializerOptions(JsonSerializerDefaults.Web)));

#endif

                });
            });
        }
    }
}
