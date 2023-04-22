// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Runtime.InteropServices;
using Serilog.Core;
using Serilog.Events;

namespace Fluxzy.Desktop.Ui.Logging
{
    class EnvironmentInformationEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "TargetId", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" 
                ? $"{Environment.MachineName}/{Environment.UserDomainName}"
                : UidProvider.Current()));

            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "AppVersion", "v0.1.18"));
        }
    }
}
