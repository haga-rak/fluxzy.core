// Copyright © 2022 Haga Rakotoharivelo

namespace Fluxzy.Desktop.Services.Models
{
    public class UiState
    {
        public UiState(FileState fileState, ProxyState proxyState, FluxzySettingsHolder settingsHolder) 
        {
            FileState = fileState;
            ProxyState = proxyState;
            SettingsHolder = settingsHolder;
        }

        public FileState FileState { get;  }

        public ProxyState ProxyState { get;  }

        public FluxzySettingsHolder SettingsHolder { get;  }
    }
}