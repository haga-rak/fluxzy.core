// Copyright © 2022 Haga Rakotoharivelo

using System.Reactive.Linq;
using Fluxzy.Desktop.Services.Hubs;
using Fluxzy.Desktop.Services.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualBasic;

namespace Fluxzy.Desktop.Services
{
    public class UiStateManager
    {
        private readonly IHubContext<GlobalHub> _hub;
        private UiState _uiState;
        
        public UiStateManager(
            IObservable<FileState> fileState,
            IObservable<ProxyState> proxyState,
            IObservable<FluxzySettingsHolder> settingHolder,
            IHubContext<GlobalHub> hub)
        {
            _hub = hub;
            var globalState = fileState.CombineLatest(
                proxyState,
                settingHolder,
                (f, p, s) => new UiState(fileState: f, proxyState: p, settingsHolder: s));

            globalState
                .Do(uiState => _uiState = uiState)
                .Do(async uiState =>
                {
                    await hub.Clients.All.SendAsync("uiUpdate", uiState);
                })
                .Subscribe();
        }

        public UiState GetUiState()
        {
            return _uiState;
        }
    }
}