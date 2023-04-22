// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Serilog.Core;
using Serilog.Events;

namespace Fluxzy.Desktop.Ui.Logging
{
    class DeletePropertiesEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent le, ILogEventPropertyFactory lepf)
        {
            le.RemovePropertyIfPresent("ConnectionId");
            le.RemovePropertyIfPresent("RequestId");
            le.RemovePropertyIfPresent("SourceContext");
            le.RemovePropertyIfPresent("EventId");
            le.RemovePropertyIfPresent("addresses");
        }
    }
}
