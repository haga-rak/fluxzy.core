// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fluxzy.Desktop.Services.Models;
using Fluxzy.Desktop.Services.Ui;
using Fluxzy.Rules;

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
            IObservable<List<Rule>> activeRulesObservable,
            IObservable<LastOpenFileState> lastOpenFileStateObservable,
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
                lastOpenFileStateObservable,
                (
                    f, p, s, sp, v, tt,
                    aro, lop) => {
                    var defaultToolBarFilters = toolBarFilterProvider.GetDefault().ToList();

                    return new UiState(f, p,
                        s, sp,
                        v, defaultToolBarFilters,
                        tt, aro, lop);
                });

            Observable = _stateObservable
                         .AsObservable()
                         .Where(s => s != null)
                         .Select(s => s!);

            _state
                .Throttle(TimeSpan
                    .FromMilliseconds(10)) // There are many case where multiple changes may occur in a short time
                .Do(uiState => _stateObservable.OnNext(uiState))
                .Do(uiState => forwardMessageManager.Send(uiState))
                .Subscribe();
        }

        public IObservable<UiState> Observable { get; }

        public async Task<UiState> GetUiState()
        {
            return await Observable.FirstAsync();
        }
    }
}
