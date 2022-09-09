// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Fluxzy.Desktop.Services.Hubs;
using Fluxzy.Desktop.Services.Models;
using Microsoft.AspNetCore.SignalR;

namespace Fluxzy.Desktop.Services
{
    public class UiStateManager 
    {
        private readonly IObservable<UiState> _state;
        private readonly BehaviorSubject<UiState?> _stateObservable = new(null); 

        public UiStateManager(
            IObservable<FileState> fileState,
            IObservable<ProxyState> proxyState,
            IObservable<FluxzySettingsHolder> settingHolder,
            IObservable<SystemProxyState> systemProxySate,

            IHubContext<GlobalHub> hub)
        {
            _state = fileState.CombineLatest(
                proxyState,
                settingHolder,
                systemProxySate,
                (f, p, s,sp) => new UiState(fileState: f, proxyState: p, settingsHolder: s, systemProxyState: sp));

            Observable = _stateObservable
                .AsObservable()
                .Where(s => s != null)
                .Select(s => s!); 

            _state
                .Do(uiState => _stateObservable.OnNext(uiState))
                .Select(uiState => hub.Clients.All.SendAsync("uiUpdate", uiState).ToObservable())
                .Switch()
                .Subscribe();
        }
        
        public IObservable<UiState> Observable { get; }

        public async Task<UiState> GetUiState()
        {
            return await Observable.FirstAsync();
        }
    }
}