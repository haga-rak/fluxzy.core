// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Desktop.Services.Models
{
    public class UiState
    {
        public FileState? FileStateState { get; set; }

        public ProxyState ProxyState { get; set; }

        public FluxzySettingsHolder SettingsHolder { get; set; }
    }
}