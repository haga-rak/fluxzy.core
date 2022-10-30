// Copyright © 2022 Haga Rakotoharivelo

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Rules;

namespace Fluxzy.Desktop.Services
{
    public class UiStateManager
    {
        private readonly IObservable<UiState> _state;
        private readonly BehaviorSubject<UiState?> _stateObservable = new(null);

        public IObservable<UiState> Observable { get; }

        public UiStateManager(
            IObservable<FileState> fileState,
            IObservable<ProxyState> proxyState,
            IObservable<FluxzySettingsHolder> settingHolder,
            IObservable<SystemProxyState> systemProxySate,
            IObservable<ViewFilter> viewFilter,
            IObservable<TemplateToolBarFilterModel> templateToolBarFilterModel,
            IObservable<List<Rule>> activeRulesObservable,
            ForwardMessageManager forwardMessageManager,
            ToolBarFilterProvider toolBarFilterProvider)
        {
            _state = fileState.CombineLatest(
                proxyState,
                settingHolder,
                systemProxySate,
                viewFilter,
                templateToolBarFilterModel,
                activeRulesObservable,
                (f, p, s, sp, v, tt, aro) =>
                {
                    var defaultToolBarFilters = toolBarFilterProvider.GetDefault().ToList();

                    return new UiState(f, p,
                        s, sp,
                        v, defaultToolBarFilters,
                        tt, aro);
                });

            Observable = _stateObservable
                         .AsObservable()
                         .Where(s => s != null)
                         .Select(s => s!);

            _state
                .Throttle(TimeSpan.FromMilliseconds(10))
                .Do(uiState => _stateObservable.OnNext(uiState))
                .Do(uiState => forwardMessageManager.Send(uiState))
                .Subscribe();
        }

        public async Task<UiState> GetUiState()
        {
            return await Observable.FirstAsync();
        }
    }
}
