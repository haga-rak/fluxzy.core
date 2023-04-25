// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Desktop.Services.Models
{
    public class AppVersion
    {
        public AppVersion(string global, string fluxzyCore, string fluxzyDesktop)
        {
            Global = global;
            FluxzyCore = fluxzyCore;
            FluxzyDesktop = fluxzyDesktop;
        }

        public string Global { get; }

        public string FluxzyCore { get; }

        public string FluxzyDesktop { get; }
    }
}
