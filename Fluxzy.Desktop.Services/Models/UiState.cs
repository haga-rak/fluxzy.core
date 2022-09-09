// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Desktop.Services.Models
{
    public class UiState
    {
        public UiState(FileState fileState, ProxyState proxyState, FluxzySettingsHolder settingsHolder, SystemProxyState systemProxyState) 
        {
            FileState = fileState;
            ProxyState = proxyState;
            SettingsHolder = settingsHolder;
            SystemProxyState = systemProxyState;
        }

        public Guid Id { get; } = Guid.NewGuid();

        public FileState FileState { get;  }

        public ProxyState ProxyState { get; }

        public SystemProxyState SystemProxyState { get; }

        public FluxzySettingsHolder SettingsHolder { get;  }

        
    }
}