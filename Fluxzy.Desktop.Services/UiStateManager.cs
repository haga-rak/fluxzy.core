// Copyright © 2022 Haga Rakotoharivelo

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
            IObservable<ViewFilter> viewFilter, 
            IObservable<TemplateToolBarFilterModel> templateToolBarFilterModel,
            IHubContext<GlobalHub> hub,

            ToolBarFilterProvider toolBarFilterProvider)
        {
            _state = fileState.CombineLatest(
                proxyState,
                settingHolder,
                systemProxySate,
                viewFilter,
                templateToolBarFilterModel,
                (f, p, s,sp, v, tt) =>
                {
                    var defaultToolBarFilters = toolBarFilterProvider.GetDefault().ToList();
                    return new UiState(fileState: f, proxyState: p, settingsHolder: s, systemProxyState: sp,
                        viewFilter: v, toolBarFilters: defaultToolBarFilters, templateToolBarFilterModel:tt);
                });

            Observable = _stateObservable
                .AsObservable()
                .Where(s => s != null)
                .Select(s => s!);

            _state
                .Throttle(TimeSpan.FromMilliseconds(10))
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