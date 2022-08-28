// Copyright © 2022 Haga Rakotoharivelo

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Desktop.Services.Models;

namespace Fluxzy.Desktop.Services
{
    public class UiStateManager
    {
        private UiState _uiState;
        
        public UiStateManager(
            IObservable<FileState> fileState,
            IObservable<ProxyState> proxyState,
            IObservable<FluxzySettingsHolder> settingHolder)
        {
            var globalState = fileState.CombineLatest(
                proxyState,
                settingHolder,
                (f, p, s) => new UiState(fileState: f, proxyState: p, settingsHolder: s));

            globalState
                .Do(uiState => _uiState = uiState).Subscribe();
        }

        public UiState GetUiState()
        {
            return _uiState;
        }
    }
}